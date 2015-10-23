using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Authentication.Models
{
    public class LoginResult
    {
        public bool Success { get; set; }

        public string Token { get; set; }

        public string ErrorMsg { get; set; }
    }
}
