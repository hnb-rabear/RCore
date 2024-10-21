namespace RCore
{
    public interface IEncryption
    {
        string Encrypt(string value);
        string Decrypt(string value);
    }

    public class Encryption : IEncryption
    {
        private static Encryption m_Singleton { get; set; }
        public static Encryption Singleton => m_Singleton ??= new Encryption();
        private byte[] m_EncryptKey;

        public Encryption()
        {
            m_EncryptKey = new byte[] {
                168, 220, 184, 133, 78, 149, 8, 249, 171, 138, 98, 170, 95, 15, 211, 200, 51, 242, 4, 193, 219, 181, 232, 99, 16, 240, 142, 128, 29, 163, 245, 24, 204, 73, 173, 32, 214, 76, 31, 99, 91, 239, 232, 53, 138, 195, 93, 195, 185, 210, 155, 184, 243, 216, 204, 42, 138, 101, 100, 241, 46, 145, 198, 66, 11, 17, 19, 86, 157, 27, 132, 201, 246, 112, 121, 7, 195, 148, 143, 125, 158, 29, 184, 67, 187, 100, 31, 129, 64, 130, 26, 67, 240, 128, 233, 129, 63, 169, 5, 211, 248, 200, 199, 96, 54, 128, 111, 147, 100, 6, 185, 0, 188, 143, 25, 103, 211, 18, 17, 249, 106, 54, 162, 188, 25, 34, 147, 3, 222, 61, 218, 49, 164, 165, 133, 12, 65, 92, 48, 40, 129, 76, 194, 229, 109, 76, 150, 203, 251, 62, 54, 251, 70, 224, 162, 167, 183, 78, 103, 28, 67, 183, 23, 80, 156, 97, 83, 164, 24, 183, 81, 56, 103, 77, 112, 248, 4, 168, 5, 72, 109, 18, 75, 219, 99, 181, 160, 76, 65, 16, 41, 175, 87, 195, 181, 19, 165, 172, 138, 172, 84, 40, 167, 97, 214, 90, 26, 124, 0, 166, 217, 97, 246, 117, 237, 99, 46, 15, 141, 69, 4, 245, 98, 73, 3, 8, 161, 98, 79, 161, 127, 19, 55, 158, 139, 247, 39, 59, 72, 161, 82, 158, 25, 65, 107, 173, 5, 255, 53, 28, 179, 182, 65, 162, 17
            };
        }

        public Encryption(byte[] encryptKey)
        {
            m_EncryptKey = encryptKey;
        }

        public string Encrypt(string value)
        {
            var plainTextBytes = XOR(System.Text.Encoding.UTF8.GetBytes(value), m_EncryptKey);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public string Decrypt(string value)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(value);
            return System.Text.Encoding.UTF8.GetString(XOR(base64EncodedBytes, m_EncryptKey));
        }

        private static byte[] XOR(byte[] input, byte[] key)
        {
            if (key == null || key.Length == 0)
            {
                return input;
            }
            byte[] output = new byte[input.Length];
            int kpos = 0;
            int kup = 0;
            int kk = 0;
            for (int pos = 0, n = input.Length; pos < n; ++pos)
            {
                output[pos] = (byte)(input[pos] ^ key[kpos] ^ kk);
                ++kpos;
                if (kpos >= key.Length)
                {
                    kpos = 0;
                    kup = (kup + 1) % key.Length;
                    kk = key[kup];
                }
            }
            return output;
        }
    }
}