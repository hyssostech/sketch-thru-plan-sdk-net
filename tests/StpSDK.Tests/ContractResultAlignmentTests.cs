using Newtonsoft.Json.Linq;
using NUnit.Framework;
using StpSDK;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace StpSDK.Tests;

/// <summary>
/// STP-1799: ImportPlanData* results and SwitchRoleAndCoa as the 0.9.11 contract has them.
/// </summary>
[TestFixture]
public class ContractResultAlignmentTests
{
    private ExtendedStubConnector _connector;
    private StpRecognizer _recognizer;

    [SetUp]
    public void Setup()
    {
        _connector = new ExtendedStubConnector();
        _recognizer = new StpRecognizer(_connector);
    }

    [TearDown]
    public void TearDown()
    {
        _recognizer.Dispose();
        _connector?.Dispose();
    }

    private static readonly ObjectSet OneObject = new(new() { new StpObject { Type = "unit", Poid = "u1" } });

    private Task<bool> Import(string form) => form switch
    {
        "string" => _recognizer.ImportSTPDataAsync("content"),
        "objectset" => _recognizer.ImportSTPDataAsync(OneObject),
        "timeout" => _recognizer.ImportSTPDataAsync(OneObject, TimeSpan.FromSeconds(30)),
        _ => throw new ArgumentException(form),
    };

    // The contract answers ImportPlanData / ImportPlanDataFromObjectSet with null on success. The connector
    // hands that on as "" (a JSON null result) or null (no result key).
    [TestCase("string", "")]
    [TestCase("string", null)]
    [TestCase("objectset", "")]
    [TestCase("objectset", null)]
    [TestCase("timeout", "")]
    [TestCase("timeout", null)]
    public async Task ImportSTPData_NullContractAnswer_IsSuccess(string form, string? answer)
    {
        _connector.NextResponse = answer;

        Assert.That(await Import(form), Is.True);
    }

    [TestCase("string")]
    [TestCase("objectset")]
    [TestCase("timeout")]
    public void ImportSTPData_Refusal_Throws(string form)
    {
        _connector.NextError = new StpException("ImportPlanData: no active scenario", null);

        Assert.That(async () => await Import(form), Throws.InstanceOf<StpException>());
    }

    [Test]
    public void IsImportAccepted_FollowsTheContract()
    {
        Assert.That(StpRecognizer.IsImportAccepted(""), Is.True);
        Assert.That(StpRecognizer.IsImportAccepted(null), Is.True);
        Assert.That(StpRecognizer.IsImportAccepted("  "), Is.True);
        Assert.That(StpRecognizer.IsImportAccepted("True"), Is.True);
        Assert.That(StpRecognizer.IsImportAccepted("False"), Is.False);
    }

    private static string[] ParamNames(JObject sent) =>
        ((JObject)sent["params"]).Properties().Select(p => p.Name).ToArray();

    [Test]
    public async Task SwitchRoleAndCoaAsync_SendsTheContractCalls_InOrder()
    {
        _connector.NextResponse = "";

        await _recognizer.SwitchRoleAndCoaAsync(StpRole.S3, "idCOA1");

        Assert.That(_connector.SentMessages, Has.Count.EqualTo(2));
        var role = JObject.Parse(_connector.SentMessages[0]);
        var coa = JObject.Parse(_connector.SentMessages[1]);
        Assert.That((string)role["method"], Is.EqualTo("SetRole"));
        Assert.That(ParamNames(role), Is.EqualTo(new[] { "role" }));
        Assert.That((string)role["params"]["role"], Is.EqualTo("S3"));
        Assert.That((string)coa["method"], Is.EqualTo("SetCurrentCoa"));
        Assert.That(ParamNames(coa), Is.EqualTo(new[] { "poid" }));
        Assert.That((string)coa["params"]["poid"], Is.EqualTo("idCOA1"));
        Assert.That(_connector.SentMessages.Any(m => m.Contains("SwitchRoleAndCoa") || m.Contains("coaPoid")), Is.False);
    }

    [TestCase(null)]
    [TestCase("")]
    public async Task SwitchRoleAndCoaAsync_WithoutCoa_SendsSetRoleOnly(string? coaPoid)
    {
        await _recognizer.SwitchRoleAndCoaAsync(StpRole.S2, coaPoid);

        Assert.That(_connector.SentMessages, Has.Count.EqualTo(1));
        Assert.That((string)JObject.Parse(_connector.SentMessages[0])["method"], Is.EqualTo("SetRole"));
    }

    [Test]
    public void SwitchRoleAndCoaAsync_RefusedRole_DoesNotSwitchCoa()
    {
        _connector.NextError = new StpException("SetRole: unknown role 'XO'", null);

        Assert.That(async () => await _recognizer.SwitchRoleAndCoaAsync("XO", "idCOA1"), Throws.InstanceOf<StpException>());
        Assert.That(_connector.SentMessages, Has.Count.EqualTo(1));
        Assert.That((string)JObject.Parse(_connector.SentMessages[0])["method"], Is.EqualTo("SetRole"));
    }
}
