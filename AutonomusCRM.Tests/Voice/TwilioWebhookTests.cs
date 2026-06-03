using AutonomusCRM.Application.Voice;

namespace AutonomusCRM.Tests.Voice;

public class TwilioWebhookTests
{
    [Fact]
    public void VoiceCallLog_UpdateFromWebhook_SetsTranscriptReady()
    {
        var log = VoiceCallLog.Create(Guid.NewGuid(), "+1", "inbound", 0, "ringing", externalCallId: "CA123");
        log.UpdateFromWebhook("completed", 90);
        Assert.Equal(90, log.DurationSeconds);
        Assert.Equal("ready_for_transcription", log.TranscriptStatus);
    }

    [Fact]
    public void TwilioPayload_HoldsCallSid()
    {
        var p = new TwilioCallWebhookPayload("CA1", "+507", "+1", "completed", 60);
        Assert.Equal("CA1", p.CallSid);
        Assert.Equal(60, p.CallDuration);
    }
}
