using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Immutable;
using System.Linq;

namespace REstomp.Middleware
{
    using StompCommand = StompParser.Command;
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class SessionMiddleware
    {
        private SessionOptions Options { get; }

        public SessionMiddleware(SessionOptions options = null)
        {
            Options = options ?? new SessionOptions();
        }

        public AppFunc Invoke(AppFunc next) =>
            async environment =>
            {
                var stompCommand = (string)environment["stomp.requestMethod"];

                if(StompCommand.IsConnectRequest(stompCommand))
                {
                    string version;
                    object versionObject;
                    if(!environment.TryGetValue("stomp.protocolVersion", out versionObject))
                        version = "1.0";
                    else
                        version = versionObject as string;

                    if(Options.AcceptedVersions.Contains(version))
                    {
                        var sessionId = Guid.NewGuid().ToString();
                        new StompFrame(StompCommand.CONNECTED, new Dictionary<string, string>
                        {
                            ["version"] = version,
                            ["session"] = sessionId

                        }).WriteToEnvironmentResponse(environment);

                        //Give other middleware a chance to disagree.
                        await next(environment);

                        //If there wasn't a disagreement.
                        if((string)environment["stomp.responseMethod"] == StompCommand.CONNECTED)
                        {
                            Options.AddSession(sessionId);
                        }
                    }
                    else
                    {
                        new StompFrame(StompCommand.ERROR, new Dictionary<string, string>
                        {
                            { "message", "No acceptable protocol version negotiated." }
                        }).WriteToEnvironmentResponse(environment);
                    }
                }
                else
                {
                    var headers = (ImmutableArray<KeyValuePair<string, string>>)environment["stomp.requestHeaders"];

                    string sessionId;

                    if(headers.TryGetValue("session", out sessionId) && !string.IsNullOrWhiteSpace(sessionId))
                    {
                        if(Options.ValidateSession(sessionId))
                            await next(environment);
                        else
                            new StompFrame(StompCommand.ERROR, new Dictionary<string, string>
                            {
                                { "message", "Invalid or expired/rejected session identifier." }
                            }).WriteToEnvironmentResponse(environment);
                    }
                    else
                    {
                        new StompFrame(StompCommand.ERROR, new Dictionary<string, string>{
                            { "message", "Session not established." }
                        }).WriteToEnvironmentResponse(environment);
                    }
                }
            };
    }

    public class SessionOptions
    {
        /// <summary>
        /// Versions of STOMP this application will accept. Defaults to 1.2 only.
        /// </summary>
        /// <returns>Accepted STOMP versions</returns>
        public string[] AcceptedVersions { get; set; } = { "1.2" };

        public Action<string> AddSession { get; set; } = (sessionId) =>
            SessionIdentifiers.Add(sessionId);

        public Func<string, bool> ValidateSession { get; set; } = (sessionId) =>
            SessionIdentifiers.Contains(sessionId);

        private static readonly IList<string> SessionIdentifiers = new List<string>();
    }
}