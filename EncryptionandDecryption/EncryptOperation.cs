using DevExpress.XtraEditors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WINDecrypt
{
    class EncryptOperation
    {
        private const ulong FC_TAG = 0xFC010203040506CF;

        private const int BUFFER_SIZE = 1024 * 1024;

        /// <summary>
        /// 检验两个Byte数组是否相同
        /// </summary>
        /// <param name="b1">Byte数组</param>
        /// <param name="b2">Byte数组</param>
        /// <returns>true－相等</returns>
        private static bool CheckByteArrays(byte[] b1, byte[] b2)
        {
            if (b1.Length == b2.Length)
            {
                for (int i = 0; i < b1.Length; ++i)
                {
                    if (b1[i] != b2[i])
                        return false;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="password">密码</param>
        /// <param name="salt"></param>
        /// <returns>加密对象</returns>
        private static SymmetricAlgorithm CreateRijndael(string password, byte[] salt)
        {
            PasswordDeriveBytes pdb = new PasswordDeriveBytes(password, salt, "SHA256", 1000);

            SymmetricAlgorithm sma = Rijndael.Create();
            sma.KeySize = 256;
            sma.Key = pdb.GetBytes(32);
            sma.Padding = PaddingMode.PKCS7;
            return sma;
        }

        /// <summary>
        /// 加密文件随机数生成
        /// </summary>
        private static RandomNumberGenerator rand = new RNGCryptoServiceProvider();

        /// <summary>
        /// 生成指定长度的随机Byte数组
        /// </summary>
        /// <param name="count">Byte数组长度</param>
        /// <returns>随机Byte数组</returns>
        private static byte[] GenerateRandomBytes(int count)
        {
            byte[] bytes = new byte[count];
            rand.GetBytes(bytes);
            return bytes;
        }

        /// <summary>
        /// 加密文件
        /// </summary>
        /// <param name="inFile">待加密文件</param>
        /// <param name="outFile">加密后输入文件</param>
        /// <param name="password">加密密码</param>
        public static async Task EncryptFile(string inFile, string outFile, string password, IProgress<double> progress,CancellationTokenSource token)
        {
            await Task.Run(() =>
              {
                  try
                  {
                      using (FileStream fin = File.OpenRead(inFile), fout = File.OpenWrite(outFile))
                      {
                          long lSize = fin.Length; // 输入文件长度
                          byte[] bytes = new byte[BUFFER_SIZE]; // 缓存
                          int read = -1; // 输入文件读取数量
                          long value = 0;

                          // 获取IV和salt
                          byte[] IV = GenerateRandomBytes(16);
                          byte[] salt = GenerateRandomBytes(16);

                          // 创建加密对象
                          SymmetricAlgorithm sma = EncryptOperation.CreateRijndael(password, salt);
                          sma.IV = IV;

                          // 在输出文件开始部分写入IV和salt
                          fout.Write(IV, 0, IV.Length);
                          fout.Write(salt, 0, salt.Length);

                          // 创建散列加密
                          HashAlgorithm hasher = SHA256.Create();
                          using (CryptoStream cout = new CryptoStream(fout, sma.CreateEncryptor(), CryptoStreamMode.Write),
                              chash = new CryptoStream(Stream.Null, hasher, CryptoStreamMode.Write))
                          {
                              BinaryWriter bw = new BinaryWriter(cout);
                              bw.Write(lSize);

                              bw.Write(FC_TAG);

                              // 读写字节块到加密流缓冲区
                              while ((read = fin.Read(bytes, 0, bytes.Length)) != 0)
                              {
                                  cout.Write(bytes, 0, read);
                                  chash.Write(bytes, 0, read);
                                  value += read;
                                  progress.Report(((double)value / (double)lSize) * 100.00);
                              }

                              // 关闭加密流
                              chash.Flush();
                              chash.Close();

                              // 读取散列
                              byte[] hash = hasher.Hash;

                              // 输入文件写入散列
                              cout.Write(hash, 0, hash.Length);

                              fin.Flush();
                              fin.Close();

                              // 关闭文件流
                              cout.Flush();
                              fout.Flush();

                              cout.Close();
                              fout.Close();
                          }
                      }
                  }
                  catch (Exception ex)
                  {
                      token.Cancel();
                  }
              },token.Token);
        }

        /// <summary>
        /// 解密文件
        /// </summary>
        /// <param name="inFile">待解密文件</param>
        /// <param name="outFile">解密后输出文件</param>
        /// <param name="password">解密密码</param>
        public static async Task DecryptFile(string inFile, string outFile, string password, IProgress<double> progress,CancellationTokenSource token)
        {
            await Task.Run(() => {
                try
                {
                    // 创建打开文件流
                    using (FileStream fin = File.OpenRead(inFile),
                        fout = File.OpenWrite(outFile))
                    {
                        int size = (int)fin.Length;
                        byte[] bytes = new byte[BUFFER_SIZE];
                        int read = -1;
                        int value = 0;
                        int outValue = 0;

                        byte[] IV = new byte[16];
                        fin.Read(IV, 0, 16);
                        byte[] salt = new byte[16];
                        fin.Read(salt, 0, 16);

                        SymmetricAlgorithm sma = EncryptOperation.CreateRijndael(password, salt);
                        sma.IV = IV;

                        value = 32;
                        long lSize = -1;

                        // 创建散列对象, 校验文件
                        HashAlgorithm hasher = SHA256.Create();

                        using (CryptoStream cin = new CryptoStream(fin, sma.CreateDecryptor(), CryptoStreamMode.Read),
                            chash = new CryptoStream(Stream.Null, hasher, CryptoStreamMode.Write))
                        {
                            // 读取文件长度
                            BinaryReader br = new BinaryReader(cin);
                            lSize = br.ReadInt64();
                            ulong tag = br.ReadUInt64();

                            if (FC_TAG != tag)
                            {
                                progress.Report(888);
                                Debug.WriteLine("The file is damaged");
                                return;
                            }

                            long numReads = lSize / BUFFER_SIZE;

                            long slack = (long)lSize % BUFFER_SIZE;

                            for (int i = 0; i < numReads; ++i)
                            {
                                read = cin.Read(bytes, 0, bytes.Length);
                                fout.Write(bytes, 0, read);
                                chash.Write(bytes, 0, read);
                                value += read;
                                outValue += read;
                                progress.Report(((double)value / (double)lSize) * 100.00);
                            }

                            if (slack > 0)
                            {
                                read = cin.Read(bytes, 0, (int)slack);
                                fout.Write(bytes, 0, read);
                                chash.Write(bytes, 0, read);
                                value += read;
                                outValue += read;
                                progress.Report(((double)value / (double)lSize) * 100.00);
                            }

                            chash.Flush();
                            chash.Close();

                            fout.Flush();
                            fout.Close();

                            byte[] curHash = hasher.Hash;

                            // 获取比较和旧的散列对象
                            byte[] oldHash = new byte[hasher.HashSize / 8];
                            read = cin.Read(oldHash, 0, oldHash.Length);
                            if ((oldHash.Length != read) || (!CheckByteArrays(oldHash, curHash)))
                            {
                                XtraMessageBox.Show("file is damaged");
                                token.Cancel();
                            }
                            cin.Flush();
                            fin.Flush();
                            fin.Close();
                            cin.Close();
                        }

                        if (outValue != lSize)
                        {
                            XtraMessageBox.Show("decrypt is failed");
                            token.Cancel();
                        }
                    }
                }
                catch (Exception ex)
                {
                    XtraMessageBox.Show($"decrypt is failed=>{ex.Message}");
                    token.Cancel();
                }
            },token.Token);
        }
    }
}
