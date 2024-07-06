﻿using Aogami.SMTV.Encryption;
using System.Data;
using System.Text;

namespace Aogami.SMTV.SaveData
{
    public class SMTVGameSaveData
    {
        private static readonly Dictionary<int, int> GAME_DATA_LENGTH_GAME_VERSION_DICT = new Dictionary<int, int>()
        {
            { 449680, 1 }, // Vengance
            { 398128, 0 }  // Base game
        };

        private readonly AesEncryption _aesManager;
        private readonly string _fileName;

        private byte[] decryptedData;

        public string FileName => _fileName;

        public int saveFileVersion;

        public static async Task<SMTVGameSaveData?> Create(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return null;
            }

            byte[] encryptedData = await File.ReadAllBytesAsync(fileName);
            if (!GAME_DATA_LENGTH_GAME_VERSION_DICT.ContainsKey(encryptedData.Length) || !DataIsEncrypted(encryptedData))
            {
                return null;
            }

            AesEncryption aesManager = new();
            byte[] decryptedData = await aesManager.Decrypt(encryptedData);
            return new(aesManager, fileName, decryptedData, GAME_DATA_LENGTH_GAME_VERSION_DICT[encryptedData.Length]);
        }

        public static async Task<SMTVGameSaveData?> Load(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return null;
            }

            
            // Todo: I dont think DataIsEncrypted works as designed for Vengence data. I dont have access to base game saves to test.
            byte[] encryptedData = await File.ReadAllBytesAsync(fileName);
            if (!GAME_DATA_LENGTH_GAME_VERSION_DICT.ContainsKey(encryptedData.Length) /*|| !DataIsEncrypted(encryptedData)*/)
            {
                return null;
            }

            AesEncryption aesManager = new();
            byte[] decryptedData = await File.ReadAllBytesAsync(fileName);
            return new(aesManager,fileName, decryptedData, GAME_DATA_LENGTH_GAME_VERSION_DICT[decryptedData.Length]);
        }



        private SMTVGameSaveData(AesEncryption aesManager, string fileName, byte[] decryptedData, int saveFileVersion)
        {
            _aesManager = aesManager;
            _fileName = fileName;
            this.decryptedData = decryptedData;
            this.saveFileVersion = saveFileVersion;
        }

        public async Task<byte> SaveDataAsync(bool makeBackUp)
        {
            byte output = 2;

            if (makeBackUp)
            {
                if (File.Exists(_fileName))
                {
                    string backUpFileName = $"{_fileName}_{DateTime.Now:yyyy-MM-dd_HH.mm.ss}.bak";
                    using FileStream sourceStream = new(_fileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
                    using FileStream destinationStream = new(backUpFileName, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
                    await sourceStream.CopyToAsync(destinationStream);
                    output += 2;
                }
                else
                {
                    output += 1;
                }
            }

            byte[] encryptedData = await _aesManager.Encrypt(decryptedData);
            await File.WriteAllBytesAsync(_fileName, encryptedData);
            return output;
        }

        public async Task<bool> ImportDecryptedData(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return false;
            }

            byte[] dataToImport = await File.ReadAllBytesAsync(fileName);
            if (!GAME_DATA_LENGTH_GAME_VERSION_DICT.ContainsKey(dataToImport.Length) || DataIsEncrypted(dataToImport))
            {
                return false;
            }

            decryptedData = dataToImport;
            return true;
        }

        public async Task ExportDecryptedData(string fileName)
        {
            await File.WriteAllBytesAsync(fileName, decryptedData);
        }

        public static bool DataIsEncrypted(byte[] data)
        {
            if (data.Length < 4 || data[0] != 0x47 || data[1] != 0x56 || data[2] != 0x41 || data[3] != 0x53)
            {
                return true;
            }

            return false;
        }

        public byte RetrieveByte(int Offset)
        {
            return decryptedData[Offset];
        }

        public string RetrieveString(int Offset, int Length, bool ValidateNameLength = false)
        {
            string output = Encoding.Unicode.GetString(decryptedData, Offset, Length);//.Replace("\0", "");
            if (ValidateNameLength && output.Length > Length / 2) output = output[..(Length / 2)];
            return output;
        }

        public short RetrieveInt16(int Offset)
        {
            return BitConverter.ToInt16(decryptedData, Offset);
        }

        public ushort RetrieveUInt16(int Offset)
        {
            return BitConverter.ToUInt16(decryptedData, Offset);
        }

        public int RetrieveInt32(int Offset)
        {
            return BitConverter.ToInt32(decryptedData, Offset);
        }

        public long RetrieveInt64(int Offset)
        {
            return BitConverter.ToInt64(decryptedData, Offset);
        }

        public uint RetrieveUInt32(int Offset)
        {
            return BitConverter.ToUInt32(decryptedData, Offset);
        }

        public bool RetrieveBoolean(int Offset)
        {
            return decryptedData[Offset] > 0;
        }

        public void UpdateByte(int Offset, byte Byte)
        {
            decryptedData[Offset] = Byte;
        }

        public void UpdateString(int Offset, string Text, int MaxLength = 16)
        {
            byte[] textBytes = Encoding.Unicode.GetBytes(Text);
            for (int i = 0; i < textBytes.Length; i++)
            {
                decryptedData[Offset++] = textBytes[i];
            }

            if (textBytes.Length < 16)
            {
                for (int i = 0; i < MaxLength - textBytes.Length; i++)
                {
                    decryptedData[Offset++] = 0;
                    decryptedData[Offset++] = 0;
                }
            }
        }

        public void UpdateInt16(int Offset, short Value)
        {
            byte[] int16Bytes = BitConverter.GetBytes(Value);
            decryptedData[Offset++] = int16Bytes[0];
            decryptedData[Offset++] = int16Bytes[1];
        }

        public void UpdateUInt16(int Offset, ushort Value)
        {
            byte[] uInt16Bytes = BitConverter.GetBytes(Value);
            decryptedData[Offset++] = uInt16Bytes[0];
            decryptedData[Offset++] = uInt16Bytes[1];
        }

        public void UpdateInt32(int Offset, int Value)
        {
            byte[] int32Bytes = BitConverter.GetBytes(Value);
            decryptedData[Offset++] = int32Bytes[0];
            decryptedData[Offset++] = int32Bytes[1];
            decryptedData[Offset++] = int32Bytes[2];
            decryptedData[Offset++] = int32Bytes[3];
        }

        public void UpdateInt64(int Offset, long Value)
        {
            byte[] int64Bytes = BitConverter.GetBytes(Value);
            decryptedData[Offset++] = int64Bytes[0];
            decryptedData[Offset++] = int64Bytes[1];
            decryptedData[Offset++] = int64Bytes[2];
            decryptedData[Offset++] = int64Bytes[3];
            decryptedData[Offset++] = int64Bytes[4];
            decryptedData[Offset++] = int64Bytes[5];
            decryptedData[Offset++] = int64Bytes[6];
            decryptedData[Offset++] = int64Bytes[7];
        }

        public void UpdateUInt32(int Offset, ushort Value)
        {
            byte[] uInt32Bytes = BitConverter.GetBytes(Value);
            decryptedData[Offset++] = uInt32Bytes[0];
            decryptedData[Offset++] = uInt32Bytes[1];
            decryptedData[Offset++] = uInt32Bytes[2];
            decryptedData[Offset++] = uInt32Bytes[3];
        }

        public void UpdateBoolean(int Offset, bool Value)
        {
            decryptedData[Offset] = Value ? (byte)1 : (byte)0;
        }
    }
}
