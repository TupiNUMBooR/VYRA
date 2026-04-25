using System;

namespace VYRA;

public sealed class ChatSendRequestedEventArgs : EventArgs
{
    public ChatSendRequestedEventArgs(string text, bool sendScreenshot)
    {
        Text = text;
        SendScreenshot = sendScreenshot;
    }

    public string Text { get; }
    public bool SendScreenshot { get; }
}
