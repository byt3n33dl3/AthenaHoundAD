﻿using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agent.Tests.TestClasses
{
    internal class TestTokenManager : ITokenManager
    {
        public TokenResponseResult AddToken(SafeAccessTokenHandle hToken, CreateToken tokenOptions, string task_id)
        {
            throw new NotImplementedException();
        }

        public SafeAccessTokenHandle GetImpersonationContext(int id)
        {
            throw new NotImplementedException();
        }

        public int getIntegrity()
        {
            return 1;
        }

        public bool Impersonate(int i)
        {
            throw new NotImplementedException();
        }

        public string List(ServerJob job)
        {
            throw new NotImplementedException();
        }

        public bool Revert()
        {
            throw new NotImplementedException();
        }
    }
}
