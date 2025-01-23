using Connections.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Connections.Uso32
{
    public enum Uso32_Bias
    {
        b1 = 0,
        b2 = 1,
        b4 = 2,
        b8 = 3,
        b16 = 4,
        b32 = 5,
    }
    public enum Uso32_Freqs
    {
        fq20Hz = 0x00,
        fq15Hz = 0x01,
        fq12Hz = 0x02,
        fq9Hz = 0x03,
        fq6_5Hz = 0x04,
        fq4_3Hz = 0x05,
        fq3Hz = 0x06,

        fq11Hz = 0x10,
        fq8Hz = 0x11,
        fq6Hz = 0x12,
        fq4Hz = 0x13,
        fq3_0Hz = 0x14,
        fq2_5Hz = 0x15,
        fq2Hz = 0x16,
    }

    public class ProtocolUSO32
    {
        public static DriverUSO32Telesystem? StartUSO32(IConnection con, Uso32_Bias c, Uso32_Freqs fq, OnUSO32DataHandler onUSO32Data, OnResponseEvent? OnResponse = null)
        {
            DriverUSO32Telesystem? d = null;
            if (con is AbstractConnection ac)
            {
                if (!(ac.Driver is DriverUSO32Telesystem))
                {
                    d = new DriverUSO32Telesystem();
                    ac.Driver = d;
                }
                else d = ac.Driver as DriverUSO32Telesystem;
                d!.OnUSO32Data += onUSO32Data;
                Task.Run(async () =>
                {
                    con.CheckLock();
                    try
                    {
                        await ac.Open();
                        var r = await ac.Transaction(new DataReq(new byte[] { (byte)(0x90 + c), (byte)fq }, 0x01, OnResponse, -1));
                    }
                    finally
                    {
                        d!.OnUSO32Data -= onUSO32Data;
                        con.UnLock();
                        await ac.Close();
                    }
                });
            }
            return d;
        }
    }
}
