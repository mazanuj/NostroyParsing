using System;

namespace NostroyParsingLib
{
    public static class Informer
    {
        public delegate void InformMethodStr(string str);

        public static event InformMethodStr OnResultReceivedStr;

        public static void RaiseOnResultReceived(string str)
        {
            var handler = OnResultReceivedStr;
            handler?.Invoke(str);
        }

        public static void RaiseOnResultReceived(Exception ex)
        {
            var handler = OnResultReceivedStr;
            handler?.Invoke(ex.Message);
            handler?.Invoke(ex.StackTrace);
        }
    }
}