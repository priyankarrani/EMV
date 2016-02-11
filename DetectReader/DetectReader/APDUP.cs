﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DetectReader
{
   public class APDUP
    { 
        byte
            cla = 0,
            channel = 0,
            p2 = 0,
            p1 = 0;
        byte[] data = null;
        short le = -1;
        bool
            useP1 = false,
            useP2 = false,
            useChannel = false,
            useData = false,
            useCla = false,
            m_fLe = false;

        
        public APDUP()
        {
        }
 
        public APDUP(APDUP param)
        {
            // Copy field
            if (param.data != null)
                param.data.CopyTo(data, 0);
            cla = param.cla;
            channel = param.channel;
            p1 = param.p1;
            p2 = param.p2;
            le = param.le;

            // Copy flags field
            useChannel = param.useChannel;
            useCla = param.useCla;
            useData = param.useData;
            m_fLe = param.m_fLe;
            useP1 = param.useP1;
            useP2 = param.useP2;
        }

        public APDUP(byte cla, byte p1, byte p2, byte[] data, short le)
        {
            this.Class = cla;
            this.P1 = p1;
            this.P2 = p2;
            this.Data = data;
            this.Le = (byte)le;
        }
       
        public APDUP Clone()
        {
            return new APDUP(this);
        }

        /// <summary>
        /// Resets the current instance, all flags are set to false
        /// </summary>
        public void Reset()
        {
            cla = 0;
            channel = 0;
            p2 = 0;
            p1 = 0;

            data = null;
            le = -1;

            useP1 = false;
            useP2 = false;
            useChannel = false;
            useData = false;
            useCla = false;
            m_fLe = false;
        }

        
        public bool UseClass
        {
            get { return useCla; }
        }

        public bool UseChannel
        {
            get { return useChannel; }
        }

        public bool UseLe
        {
            get { return m_fLe; }
        }

        public bool UseData
        {
            get { return useData; }
        }

        public bool UseP1
        {
            get { return useP1; }
        }

        public bool UseP2
        {
            get { return useP2; }
        }
       

       
        public byte P1
        {
            get { return p1; }

            set
            {
                p1 = value;
                useP1 = true;
            }
        }

        public byte P2
        {
            get { return p2; }
            set
            {
                p2 = value;
                useP2 = true;
            }

        }

        public byte[] Data
        {
            get { return data; }
            set
            {
                data = value;
                useData = true;
            }
        }

        public byte Le
        {
            get { return (byte)le; }
            set
            {
                le = value;
                m_fLe = true;
            }
        }

        public byte Channel
        {
            get { return channel; }
            set
            {
                channel = value;
                useChannel = true;
            }
        }

        public byte Class
        {
            get { return cla; }
            set
            {
                cla = value;
                useCla = true;
            }
        }

       
    }
}
