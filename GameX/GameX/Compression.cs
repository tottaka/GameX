using System;
using System.Collections.Generic;
using K4os.Compression.LZ4;

namespace GameX
{

    public enum CompressionMethod
    {
        Lz4
    }

    public static class Compression
    {
        public static byte[] Zip(byte[] data, CompressionMethod method = CompressionMethod.Lz4)
        {
            if(method == CompressionMethod.Lz4)
                return LZ4Pickler.Pickle(data);

            return null;
            /*
            byte[] target = new byte[LZ4Codec.MaximumOutputSize(data.Length)];
            int size = LZ4Codec.Encode(data, 0, data.Length, target, 0, target.Length);
            output = target;
            return size;
            */
        }


        public static byte[] Unzip(byte[] data, CompressionMethod method = CompressionMethod.Lz4)
        {
            if(method == CompressionMethod.Lz4)
                return LZ4Pickler.Unpickle(data);

            return null;
        }

    }
}
