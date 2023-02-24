using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iMobie.Social.WhatsApp.JavaBeanParse
{
    public class ParseJavaBean
    {
        int baseWireHandle = 8257536;

        int index = 0;
        const byte TC_NULL = 112;
        const byte TC_REFERENCE = 113;
        const byte TC_CLASS = 118;
        const byte TC_CLASSDESC = 114;
        const byte TC_PROXYCLASSDESC = 125;
        const byte TC_STRING = 116;
        const byte TC_LONGSTRING = 124;
        const byte TC_ARRAY = 117;
        const byte TC_ENUM = 126;
        const byte TC_OBJECT = 115;
        const byte TC_EXCEPTION = 123;
        const byte TC_BLOCKDATA = 19;
        const byte TC_BLOCKDATALONG = 122;
        const byte TC_ENDBLOCKDATA = 120;

        byte[] javaData;

        public JsBean jsBean = new JsBean();

        public List<object> entites = new List<object>();

        public ParseJavaBean(byte[] data)
        {
            this.javaData = data;
        }

        public JsBean execute()
        {
            index = 4;
            readObject0(jsBean);
            return jsBean;
        }

        public void readObject0(JsBean jsBean)
        {
            byte tc = peekByte();
            switch (tc)
            {
                case TC_NULL:
                    break;
                case TC_OBJECT:
                    readOrdinaryObject(ref jsBean);
                    break;
                case TC_CLASSDESC:
                case TC_PROXYCLASSDESC:
                    readClassDesc(ref jsBean);
                    break;
                case TC_STRING:
                case TC_LONGSTRING:
                    jsBean.data = readString(tc);
                    break;
                case TC_ARRAY:
                    jsBean.data = readArray(ref jsBean);
                    break;
            }
        }

        public void readClassDesc(ref JsBean jsBean)
        {
            var tc = peekByte();
            switch (tc)
            {
                case TC_NULL:
                    readNull();
                    break;
                case TC_REFERENCE:
                    readHandle();
                    break;
                case TC_PROXYCLASSDESC:
                    //readProxyDesc(ref jsBean);
                    break;
                case TC_CLASSDESC:
                    readNonProxyDesc(ref jsBean);
                    break;
            }
        }

        public void readOrdinaryObject(ref JsBean jsBean)
        {

            var tc = peekByte();
            switch (tc)
            {
                case TC_NULL:

                    break;
                case TC_REFERENCE:

                    break;
                case TC_PROXYCLASSDESC:

                    break;
                case TC_CLASSDESC:
                    readNonProxyDesc(ref jsBean);
                    break;
            }
            entites.Add(jsBean);
            readSerialData(ref jsBean);
        }

        public void readNonProxyDesc(ref JsBean jsBean)
        {
            entites.Add(jsBean);
            readClassDescriptor(ref jsBean);
            skipCustomData(ref jsBean);
            readClassDesc(ref jsBean);
        }

        public void readClassDescriptor(ref JsBean jsBean)
        {
            readNonProxy(ref jsBean);


        }

        public void skipCustomData(ref JsBean jsBean)
        {
            switch (peekByte())
            {
                case TC_BLOCKDATA:
                case TC_BLOCKDATALONG:
                    break;

                case TC_ENDBLOCKDATA:
                    readByte();
                    return;
                default:
                    readObject0(jsBean);
                    break;
            }
        }

        public void readNonProxy(ref JsBean jsBean)
        {
            if (jsBean.name == null)
            {
                jsBean.name = Encoding.UTF8.GetString(readUTF());
            }
            else
            {
                jsBean.fieId = Encoding.UTF8.GetString(readUTF());
            }

            jsBean.suid = readLong();

            jsBean.flags = peekByte();
            jsBean.fieIdsCount = readShort();
            jsBean.fieIds = new List<FieId>();
            for (var i = 0; i < jsBean.fieIdsCount; i++)
            {
                char tcode = BitConverter.ToChar(javaData, index);
                index++;
                var fname = Encoding.UTF8.GetString(readUTF());
                var signature = ((tcode == 'L') || (tcode == '[')) ?
                    readTypeString() : tcode.ToString();

                jsBean.fieIds.Add(new FieId()
                {
                    name = fname,
                    sign = signature,
                    offset = 0
                });
            }
            computeFieldOffsets(ref jsBean);
        }

        public void computeFieldOffsets(ref JsBean jsBean)
        {
            jsBean.primDataSize = 0;
            jsBean.numObjFields = 0;
            jsBean.firstObjIndex = -1;

            for (var i = 0; i < jsBean.fieIds.Count; i++)
            {
                var field = jsBean.fieIds[i];
                switch (field.sign[0])
                {
                    case 'Z':
                    case 'B':
                        field.offset = jsBean.primDataSize++;
                        break;

                    case 'C':
                    case 'S':
                        field.offset = jsBean.primDataSize;
                        jsBean.primDataSize += 2;
                        break;

                    case 'I':
                    case 'F':
                        field.offset = jsBean.primDataSize;
                        jsBean.primDataSize += 4;
                        break;

                    case 'J':
                    case 'D':
                        field.offset = jsBean.primDataSize;
                        jsBean.primDataSize += 8;
                        break;

                    case '[':
                    case 'L':
                        field.offset = jsBean.numObjFields++;
                        if (jsBean.firstObjIndex == -1)
                        {
                            jsBean.firstObjIndex = i;
                        }
                        break;
                }
            }
        }

        public void readSerialData(ref JsBean jsBean)
        {
            defaultReadFields(ref jsBean);
            if (jsBean.name == "java.io.File" || jsBean.fieId == "java.io.File")
            {
                readInt();
                peekByte();
            }
            else if (jsBean.name == "java.util.ArrayList" || jsBean.fieId == "java.util.ArrayList")
            {
                if (jsBean.data == null && (int)jsBean.fieIds[0].data > 0)
                {
                    readInt();
                    readShort();
                    jsBean.data = new List<object>();
                    for (var i = 0; i < (int)jsBean.fieIds[0].data; i++)
                    {
                        JsBean jsb = new JsBean();
                        readObject0(jsb);
                        ((List<object>)jsBean.data).Add(jsb.data);
                    }

                }
            }
        }

        public string readTypeString()
        {
            var tc = peekByte();
            switch (tc)
            {
                case TC_NULL:
                    readNull();
                    return null;
                case TC_REFERENCE:

                    return (string)readHandle();
                case TC_STRING:
                case TC_LONGSTRING:
                    return readString(tc);
            }
            return null;
        }

        public short readShort()
        {
            return readUnsignedShort();
        }

        public object readHandle()
        {
            int i = readInt();
            i -= baseWireHandle;
            return entites[i];
        }

        public void defaultReadFields(ref JsBean jsBean)
        {
            jsBean.primData = javaData.Skip(index).Take(jsBean.primDataSize).ToArray();
            index += jsBean.primDataSize;
            for (var i = 0; i < jsBean.fieIds.Count; i++)
            {
                switch (jsBean.fieIds[i].sign[0])
                {
                    case 'Z':
                        jsBean.fieIds[i].data = getBoolean(jsBean.primData, jsBean.fieIds[i].offset);
                        break;

                    case 'B':
                        jsBean.fieIds[i].data = getByte(jsBean.primData, jsBean.fieIds[i].offset);
                        break;

                    case 'C':
                        jsBean.fieIds[i].data = getChar(jsBean.primData, jsBean.fieIds[i].offset);
                        break;

                    case 'S':
                        jsBean.fieIds[i].data = getShort(jsBean.primData, jsBean.fieIds[i].offset);
                        break;

                    case 'I':
                        jsBean.fieIds[i].data = getInt(jsBean.primData, jsBean.fieIds[i].offset);
                        break;

                    case 'F':
                        jsBean.fieIds[i].data = getFloat(jsBean.primData, jsBean.fieIds[i].offset);
                        break;

                    case 'J':
                        jsBean.fieIds[i].data = getLong(jsBean.primData, jsBean.fieIds[i].offset);
                        break;

                    case 'D':
                        jsBean.fieIds[i].data = getDouble(jsBean.primData, jsBean.fieIds[i].offset);
                        break;
                }
            }
            var numPrimFields = jsBean.fieIds.Count - jsBean.numObjFields;

            for (var i = 0; i < jsBean.numObjFields; i++)
            {
                readObject0(jsBean.fieIds[numPrimFields + i]);
            }

        }

        public byte[] readArray(ref JsBean jsBean)
        {
            readClassDesc(ref jsBean);
            var len = readInt();
            byte[] byteArray = javaData.Skip(index).Take(len).ToArray();
            index += len;
            entites.Add(byteArray);
            return byteArray;
        }

        public bool getBoolean(byte[] data, int position)
        {
            return data[position] != 0;
        }

        public byte getByte(byte[] data, int position)
        {
            return data[position];
        }

        public char getChar(byte[] data, int position)
        {
            char str = Bits.getChar(data, position);
            return str;
        }

        public short getShort(byte[] data, int position)
        {
            return Bits.getShort(data, position);
        }

        public int getInt(byte[] data, int position)
        {
            return Bits.getInt(data, position);
        }

        public long getLong(byte[] data, int position)
        {
            return Bits.getLong(data, position);
        }


        public double getDouble(byte[] data, int position)
        {
            byte[] doubleData = data.Skip(position).Take(8).ToArray();
            Array.Reverse(doubleData);
            return BitConverter.ToDouble(doubleData, 0);
        }

        public float getFloat(byte[] data, int position)
        {
            byte[] floatData = data.Skip(position).Take(4).ToArray();
            Array.Reverse(floatData);
            return BitConverter.ToSingle(floatData, 0);
        }

        public int readInt()
        {
            int i = Bits.getInt(javaData, index);
            index += 4;
            return i;
        }

        public void readNull()
        {
            readByte();
        }

        private byte readByte()
        {
            return javaData[index];
        }

        public string readString(byte tc)
        {
            byte[] bytes = null;
            switch (tc)
            {
                case TC_STRING:
                    bytes = readUTF();
                    break;

                case TC_LONGSTRING:
                    bytes = readLongUTF();
                    break;
            }


            string str = Encoding.UTF8.GetString(bytes);
            entites.Add(str);
            return str;
        }

        public byte[] readLongUTF()
        {
            return readUTFBody(readLong());
        }

        public long readLong()
        {
            long l = Bits.getLong(javaData, index);
            index += 8;
            return l;
        }

        public byte[] readUTF()
        {
            return readUTFBody(readUnsignedShort());
        }

        public byte[] readUTFBody(short s)
        {
            byte[] utf = javaData.Skip(index).Take(s).ToArray();
            index += s;
            return utf;
        }

        public byte[] readUTFBody(long s)
        {
            byte[] utf = javaData.Skip(index).Take((int)s).ToArray();
            index += (int)s;
            return utf;
        }

        public short readUnsignedShort()
        {
            short s = Bits.getShort(javaData, index);
            index += 2;
            return s;
        }


        public byte peekByte()
        {
            byte b = javaData[index];
            index++;
            return b;
        }
    }
}
