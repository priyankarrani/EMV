using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DetectReader
{
  public  class CardDExcep : Exception
    {
        public CardDExcep()
            : base("PC/SC exception")
        {
        }

        public CardDExcep(int result)
            : base(WinConCard.SCardErrorMessage(result))
        {
            Result = result;
        }

        public int Result { get; private set; }
    }
}
  