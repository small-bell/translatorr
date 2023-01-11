using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace translator
{
    class HttpUtils
    {
        public static String HttpGet(String url)
        {
            string strReturn = string.Empty;
            String urlEsc = url;
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(urlEsc);
            req.Method = "GET";
            try
            {
                using (WebResponse wr = req.GetResponse())
                {
                    Stream stream = wr.GetResponseStream();
                    byte[] buf = new byte[1024];
                    while (true)
                    {
                        int len = stream.Read(buf, 0, buf.Length);
                        if (len <= 0)//len <= 0 跳出循环
                            break;
                        strReturn += System.Text.Encoding.GetEncoding("utf-8").GetString(buf, 0, len);//指定编码格式
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return strReturn;
        }
    }
}
