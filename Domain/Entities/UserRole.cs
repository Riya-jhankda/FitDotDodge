using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{

    //Because UserRoles is just a static helper class, not a database table or entity. 
    public static class UserRole
    {


        public const string User = "User";
        public const string Coach = "Coach";
        public const string Admin = "Admin";
        public const string SuperAdmin = "SuperAdmin";
        //const means these values are constant and cannot be changed

    }
}
