﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace DetectReader
{
    public class APDUCmd
    {   
            private const int APDU_MIN_LENGTH = 4;

            private byte cla;
            private byte ins;
            private byte p1;
            private byte p2;
            private byte[] data;
            private byte le;

          
            public APDUCmd(byte cla, byte ins, byte p1, byte p2, byte[] data, byte le)
            {
                this.cla = cla;
                this.ins = ins;
                this.p1 = p1;
                this.p2 = p2;
                this.data = data;
                this.le = le;
            }

            #region Accessors
        
            public byte Class
            {
                get
                {
                    return cla;
                }
            }
         
          
            public byte Ins
            {
                get
                {
                    return ins;
                }
            }


          
            public byte P1
            {
                get
                {
                    return p1;
                }
            }
         
            public byte P2
            {
                get
                {
                    return p2;
                }
            }
         
            public byte[] Data
            {
                get
                {
                    return data;
                }
            }

         
            public byte Le
            {
                get
                {
                    return le;
                }
            }
            #endregion

            
            public override string ToString()
            {
                string strData = null;

                if (data != null)
                {
                    StringBuilder sb = new StringBuilder(data.Length * 2);

                    for (int i = 0; i < data.Length; i++)
                    {
                        sb.AppendFormat("{0:X02}", data[i]);
                    }

                    strData = "Data: " + sb.ToString();
                    le = (byte)data.Length;
                }

                StringBuilder sbApdu = new StringBuilder();
                sbApdu.AppendFormat("Cla: {0:X02} Ins: {1:X02} P1: {2:X02} P2: {3:X02} Le: {4:X02} ", cla, ins, p1, p2, le);

                if (data != null)
                {
                    sbApdu.Append(strData);
                }

                return sbApdu.ToString();
            }
         
            public byte[] ToArray()
            {
                byte[] buffer = null;

                if (this.Data == null)
                {
                    buffer = new byte[APDU_MIN_LENGTH + ((this.Le != 0) ? 1 : 0)];

                    if (this.Le != 0)
                    {
                        buffer[4] = (byte)this.Le;
                    }
                }
                else
                {
                    buffer = new byte[APDU_MIN_LENGTH + 1 + this.Data.Length];

                    for (int i = 0; i < this.Data.Length; i++)
                    {
                        buffer[APDU_MIN_LENGTH + 1 + i] = this.Data[i];
                    }

                    buffer[APDU_MIN_LENGTH] = (byte)this.Data.Length;
                }

                buffer[0] = this.Class;
                buffer[1] = this.Ins;
                buffer[2] = this.P1;
                buffer[3] = this.P2;

                return buffer;
            }
        }
    }
