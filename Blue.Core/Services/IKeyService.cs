using System;
using System.Collections.Generic;
using System.Text;

namespace Blue.Core.Services
{
    public interface IKeyService
    {
        public string GetKey();
        public void SetKey(string key);
    }
}
