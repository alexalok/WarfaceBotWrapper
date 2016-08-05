using System;
using System.Collections.Generic;
using System.Text;

namespace BotWrapper
{
    static class Format
    {
        public static string FormatQuery(string query, int lastQueryId, string nickname)
        {
            query = query.Replace("{{QUERY_UID}}", "uid" + lastQueryId++.ToString("00000000"));
            query = query.Replace("{{NICKNAME}}", "uid" + nickname);
            return query;
        }
    }
}
