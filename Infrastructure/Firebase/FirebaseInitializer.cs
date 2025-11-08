using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;


namespace Infrastructure.Firebase
{
    
    public static class FirebaseInitializer
    {
        public static void Initialize()
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.FromFile("Secrets/firebase-adminsdk.json")
                });
            }
        }
    }

}
