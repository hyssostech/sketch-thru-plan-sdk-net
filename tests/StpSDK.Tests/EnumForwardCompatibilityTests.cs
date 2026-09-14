using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace StpSDK.Tests;

/// <summary>
/// The SDK's forward-compatibility contract for enums on the wire.
/// </summary>
/// <remarks>
/// <para>
/// The engine serialises enums with <c>.ToString()</c>, so an engine newer than
/// this SDK sends member names that are not in these enums. The contract is that
/// an unknown name degrades to a default and the surrounding message still
/// arrives - a new task type must not take the whole event with it.
/// </para>
/// <para>
/// This was only ever covered incidentally, by one dispatch test that happened
/// to use <c>what="ATTACK"</c>. That is how it broke silently under
/// Newtonsoft.Json 13.0.4: <c>NullSafeStringEnumConverter</c> returned
/// <c>null</c> for a NON-NULLABLE enum, which 13.0.3 tolerated and 13.0.4
/// rejects - failing the whole containing object, which
/// <c>HandleTaskAdded</c> then drops because its alternates list is empty.
/// </para>
/// <para>
/// These tests name the contract directly, so a future serializer change fails
/// here with an obvious message rather than as a mysteriously missing event.
/// </para>
/// </remarks>
[TestFixture]
public class EnumForwardCompatibilityTests
{
    private sealed class WithRequiredEnum
    {
        [JsonProperty("what")]
        [JsonConverter(typeof(NullSafeStringEnumConverter))]
        public TaskWhat What { get; set; }
    }

    private sealed class WithNullableEnum
    {
        [JsonProperty("what")]
        [JsonConverter(typeof(NullSafeStringEnumConverter))]
        public TaskWhat? What { get; set; }
    }

    [Test]
    public void UnknownEnumName_OnNonNullableProperty_DegradesToDefault_AndDoesNotThrow()
    {
        // "ATTACK" is deliberately NOT a TaskWhat member.
        Assert.That(Enum.IsDefined(typeof(TaskWhat), "ATTACK"), Is.False,
                    "this test is meaningless if ATTACK becomes a real member - pick another unknown name");

        WithRequiredEnum result = null;
        Assert.DoesNotThrow(
            () => result = JObject.Parse(@"{""what"":""ATTACK""}").ToObject<WithRequiredEnum>(),
            "an unknown enum name must not break deserialisation of the containing object");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.What, Is.EqualTo(default(TaskWhat)));
    }

    [Test]
    public void UnknownEnumName_OnNullableProperty_BecomesNull()
    {
        WithNullableEnum result = null;
        Assert.DoesNotThrow(
            () => result = JObject.Parse(@"{""what"":""ATTACK""}").ToObject<WithNullableEnum>());

        Assert.That(result, Is.Not.Null);
        Assert.That(result.What, Is.Null, "null is a value a Nullable<TEnum> can actually hold");
    }

    [Test]
    public void ExplicitJsonNull_OnNonNullableProperty_DegradesToDefault()
    {
        WithRequiredEnum result = null;
        Assert.DoesNotThrow(
            () => result = JObject.Parse(@"{""what"":null}").ToObject<WithRequiredEnum>());

        Assert.That(result, Is.Not.Null);
        Assert.That(result.What, Is.EqualTo(default(TaskWhat)));
    }

    [Test]
    public void KnownEnumName_StillResolves()
    {
        // The fallback must not swallow names that ARE valid.
        var result = JObject.Parse(@"{""what"":""AMBUSH""}").ToObject<WithRequiredEnum>();

        Assert.That(result.What, Is.EqualTo(TaskWhat.AMBUSH));
        Assert.That(result.What, Is.Not.EqualTo(default(TaskWhat)),
                    "AMBUSH must differ from the default, or this test cannot tell resolution from fallback");
    }
}
