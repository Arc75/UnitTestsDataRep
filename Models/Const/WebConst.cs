using System;
using System.Collections.Generic;
using System.Text;

namespace TestMocksGenerator.Models.Const
{
    public static class WebConst
    {
        public const string AuthRequestAddr = "http://addr:8000/API/REST/Authorization/LoginWith?username=admin";
        public const string AuthToken = "longtoken to be here";
        public const string EntityRequestAddr = "http://addr:8000/API/REST/Entity/Load?type={0}&id={1}";
        public const string EntityQueryRequestAddr = "http://addr:8000/API/REST/Entity/Query?type={0}&q={1}";
    }
}
