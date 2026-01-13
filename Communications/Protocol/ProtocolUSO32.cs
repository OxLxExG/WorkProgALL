using Connections.Interface;

namespace Connections.Uso32
{
    public enum Uso32_Gain
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
        public static void StartUSO32(IConnection con, Uso32_Gain c, Uso32_Freqs fq, OnUSO32DataHandler onUSO32Data, OnResponseEvent? OnResponse = null)
        {
            if (con is AbstractConnection ac && ac.Driver is DriverUSO32Telesystem d && !ac.IsLocked)
            {
                Task.Run(async () =>
                {

                    con.CheckLock();
                    var re = new DataReq(new byte[] { (byte)(0x90 + c), (byte)fq }, 0x01, OnResponse, -1);
                    d.OnUSO32Data += onUSO32Data;
                    try
                    {
                        try
                        {
                            await ac.Open();
                        }
                        catch
                        {
                            OnResponse?.Invoke(con, new DataResp(re, -1));
                            throw;
                        }
                        var r = await ac.Transaction(re);
                    }
                    finally
                    {
                        d.OnUSO32Data -= onUSO32Data;
                        con.UnLock();
                        await ac.Close();
                    }
                });
            }
            else throw new Exception("USO32 StartUSO32");
        }
    }
}
