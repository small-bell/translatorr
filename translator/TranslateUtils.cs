using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace translator
{
    class TranslateUtils
    {
        public static string APPID = "APPID";
        public static string PASSWORD = "PASSWORD";
        public static string BASEURL = "https://fanyi-api.baidu.com/api/trans/vip/translate"
               + APPID + "&salt=1435660288&sign=";

        public static string Generate_Url(string word)
        {
            // 源语言
            string from = "en";
            // 目标语言
            string to = "zh";
            Random rd = new Random();
            string salt = rd.Next(100000).ToString();
            string sign = MD5Utils.Md5(APPID + word + salt + PASSWORD);
            string url = "http://api.fanyi.baidu.com/api/trans/vip/translate?";
            url += "q=" + HttpUtility.UrlEncode(word);
            url += "&from=" + from;
            url += "&to=" + to;
            url += "&appid=" + APPID;
            url += "&salt=" + salt;
            url += "&sign=" + sign;
            return url;
            
        }

        public static string Translate(string word)
        {
            string url = Generate_Url(word);
            string res = HttpUtils.HttpGet(url);
            TranslateResponse translateResponse =
                JsonConvert.DeserializeObject<TranslateResponse>(UnicodeUtils.UnicodeDecode(res));
            List<TranslateItem> items =  translateResponse.trans_result;
            if (items.Count > 0)
            {
                return items[0].dst;
            }
            return "未搜索到结果";
        }
    }
}
