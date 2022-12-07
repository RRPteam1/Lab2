using System.Text;

namespace SmptClient
{
    public class Utils
    {
        public static string Base64Encode(string data, Encoding e = null)
        {
            if (data == null) return "";

            if (e == null) e = Encoding.UTF8;
            byte[] buffer = e.GetBytes(data);
            return Convert.ToBase64String(buffer);
        }

        public static string Base64Decode(string base64, Encoding e = null)
        {
            if (base64 == null) return "";

            if (e == null) e = Encoding.UTF8;
            byte[] buffer = Convert.FromBase64String(base64);
            return e.GetString(buffer);
        }

        public static string Base64ExtendedWordEncode(string data, Encoding e = null)
        {
            if (data == null) return "";

            if (e == null) e = Encoding.UTF8;
            return "=?" + e.HeaderName.ToUpper() + "?B?" + Base64Encode(data, e) + "?=";
        }

        public static string RcptMerge(string[] to)
        {
            string retval = "";
            if (to == null) return retval;

            int index;
            for (index = 0; index < to.Length - 1; index++)
            {
                retval += "<" + to[index] + ">, ";
            }
            retval += "<" + to[index] + ">";
            return retval;
        }
    }
}
