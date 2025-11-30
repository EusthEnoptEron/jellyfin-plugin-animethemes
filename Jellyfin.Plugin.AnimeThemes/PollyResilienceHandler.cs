using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;

namespace Jellyfin.Plugin.AnimeThemes;

/// <summary>
/// Resilience handler that uses Polly under the hood.
/// </summary>
public sealed class PollyResilienceHandler : DelegatingHandler
{
    private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="PollyResilienceHandler"/> class.
    /// </summary>
    public PollyResilienceHandler()
    {
        _pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(GetRetryOptions())
            .Build();
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return _pipeline.ExecuteAsync(
            async token => await base.SendAsync(request, token).ConfigureAwait(false),
            cancellationToken).AsTask();
    }

    private static ValueTask<bool> HandleTransientHttpError(Outcome<HttpResponseMessage> outcome) => outcome switch
    {
        { Exception: HttpRequestException } => PredicateResult.True(),
        { Result.StatusCode: HttpStatusCode.RequestTimeout } => PredicateResult.True(),
        { Result.StatusCode: HttpStatusCode.TooManyRequests } => PredicateResult.True(),
        { Result.StatusCode: >= HttpStatusCode.InternalServerError } => PredicateResult.True(),
        _ => PredicateResult.False()
    };

    private static RetryStrategyOptions<HttpResponseMessage> GetRetryOptions() => new()
    {
        ShouldHandle = args => HandleTransientHttpError(args.Outcome),
        MaxRetryAttempts = 3,
        BackoffType = DelayBackoffType.Exponential,
        Delay = TimeSpan.FromSeconds(2),
        DelayGenerator = args => ValueTask.FromResult(
            args.Outcome.Result?.Headers.RetryAfter?.Delta)
    };
}
