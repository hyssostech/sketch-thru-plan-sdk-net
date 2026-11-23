using NUnit.Framework;
using StpSDK.Search;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace StpSDK.Tests;

/// <summary>STP-1803: the symbol index loaded from the STP bridge's <c>/symbol-index.json</c>.</summary>
[TestFixture]
public class SymbolIndexHttpTests
{
    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public Uri? Requested { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requested = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
        }
    }

    private static string Fixture() =>
        File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "SymbolSearch", "fixture-index.json"));

    [Test]
    public async Task LoadFromBridge_GetsSymbolIndexJson_AndParsesIt()
    {
        using var handler = new StubHandler(HttpStatusCode.OK, Fixture());
        using var http = new HttpClient(handler);
        var index = await SymbolIndex.LoadFromBridgeAsync("stp-host", 9555, http);

        Assert.That(handler.Requested, Is.EqualTo(new Uri("http://stp-host:9555/symbol-index.json")));
        Assert.That(new SymbolSearch(index).Search("quill sparrow")[0].Key, Is.EqualTo("Q600"));
    }

    [Test]
    public void LoadFromBridge_HttpError_IsUnavailable()
    {
        using var handler = new StubHandler(HttpStatusCode.NotFound, "");
        using var http = new HttpClient(handler);
        var ex = Assert.ThrowsAsync<SymbolIndexUnavailableException>(
            () => SymbolIndex.LoadFromBridgeAsync("stp-host", 9555, http));
        Assert.That(ex.Message, Does.Contain("http://stp-host:9555/symbol-index.json").And.Contain("HTTP 404"));
    }

    [Test]
    public void LoadFromBridge_NotAnIndex_IsUnavailable()
    {
        using var handler = new StubHandler(HttpStatusCode.OK, "{\"schema\":\"other\"}");
        using var http = new HttpClient(handler);
        Assert.ThrowsAsync<SymbolIndexUnavailableException>(
            () => SymbolIndex.LoadFromBridgeAsync("stp-host", 9555, http));
    }
}
