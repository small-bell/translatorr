using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace translator
{
    class TranslateResponse
    {
        public string from { get; set; }
        public string to { get; set; }
        public List<TranslateItem> trans_result { get; set; }
    }
}
