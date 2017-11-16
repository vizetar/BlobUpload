using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.FileProcessing
{
    public class UserImpersonation
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool LogonUser(
          string principal,
          string authority,
          string password,
          LogonSessionType logonType,
          LogonProvider logonProvider,
          out IntPtr token);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr handle);
        enum LogonSessionType : uint
        {
            Interactive = 2,
            Network,
            Batch,
            Service,
            NetworkCleartext = 8,
            NewCredentials
        }
        enum LogonProvider : uint
        {
            Default = 0, // default for platform (use this!)
            WinNT35,     // sends smoke signals to authority
            WinNT40,     // uses NTLM
            WinNT50      // negotiates Kerb or NTLM
        }

       // private WindowsImpersonationContext impersonatedUser = null;
        private IntPtr token = IntPtr.Zero;

        public bool ImpersonateUser(string userLogin, string domain, string password)
        {

			string ntUserLogin = userLogin;
			string ntUserPassword = password;

			bool result = false;
            try
            {

                if (domain == null || domain == string.Empty)
                {
                    result = LogonUser(ntUserLogin, null,
                                            ntUserPassword,
                                            LogonSessionType.NewCredentials,
                                            LogonProvider.WinNT50,
                                            out token);
                }
                else
                {
                    result = LogonUser(ntUserLogin, domain,
                                                ntUserPassword,
                                                LogonSessionType.NetworkCleartext,
                                                LogonProvider.WinNT50,
                                                out token);
                }
                
                if (result)
                {
                    WindowsIdentity id = new WindowsIdentity(token);

					// Begin impersonation
					//impersonatedUser = id.ImpersonationLevel;
                    // Log the new identity
                    // Resource access here uses the impersonated identity
                }
                else
                {
                    result = false;
                }
            }
            catch (Exception ex)
            {

                throw ex;
                result = false;
            }

            return result;
        }

        //public bool UndoImpersonation()
        //{
        //    try
        //    {
        //        if (impersonatedUser != null)
        //            impersonatedUser.Undo();
        //        // Free the token
        //        if (token != IntPtr.Zero)
        //            CloseHandle(token);

        //        return true;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}

        private string DecodeCredential(string encodedData)
        {
            System.Text.UTF8Encoding encoder = new System.Text.UTF8Encoding();
            System.Text.Decoder utf8Decode = encoder.GetDecoder();

            byte[] todecode_byte = Convert.FromBase64String(encodedData);
            int charCount = utf8Decode.GetCharCount(todecode_byte, 0, todecode_byte.Length);
            char[] decoded_char = new char[charCount];
            utf8Decode.GetChars(todecode_byte, 0, todecode_byte.Length, decoded_char, 0);
            string result = new String(decoded_char);
            return result;
        }


    }
}
