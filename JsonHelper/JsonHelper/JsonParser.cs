using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace JsonHelper
{
    internal enum JsonToken
    {
        None,
        Number,

        Null = 'n',
        True = 't',
        False = 'f',

        Object = '{',
        EndObject = '}',

        Array = '[',
        EndArray = ']',

        String = '"',
        
        Colon = ':',
        Comma = ','
    }

    public unsafe class JsonParser
    {
        private string json;
        private int index;

        [ThreadStatic]
        private static StringBuilder buffer = new StringBuilder();

        public JsonParser()
        {
        }

        public object Parse(string input)
        {
            Assert(input == null, "输入数据不能为空");

            this.json = input;
            this.index = 0;

            switch (GetToken())
            {
                case JsonToken.Object:
                    return ParseObject();

                case JsonToken.Array:
                    return ParseArray();

                default:
                    throw new ArgumentException("JSON格式错误!");
            }
        }

        private JsonToken GetToken()
        {
            char ch;
            fixed (char* p = json)
            {
                // 忽略空白符
                char* pStart = p + index;
                char* pCur = p + index;
                while (*pCur != '\0' && IsWhiteSpace(*pCur))
                {
                    pCur++;
                }

                index += (int)(pCur - pStart);
                ch = Char.ToLower(*pCur);
            }

            Assert(ch == '\0', "JSON格式错误!");

            switch (ch)
            {
                case '[':
                case ']':
                case '{':
                case '}':
                case '"':
                case ':':
                case ',':
                case 'n':
                case 't':
                case 'f':
                    return (JsonToken)ch;

                default:
                    if (ch == '-' || ch == '.' || IsNumberChar(ch))
                        return JsonToken.Number;

                    throw new ArgumentException("JSON格式错误!");
            }
        }

        private object ParseValue()
        {
            JsonToken token;
            switch ((token = GetToken()))
            {
                case JsonToken.Object:
                    return ParseObject();

                case JsonToken.Array:
                    return ParseArray();

                case JsonToken.String:
                    return ParseString();

                case JsonToken.Number:
                    return ParseNumber();

                case JsonToken.True:
                case JsonToken.False:
                case JsonToken.Null:
                    return ParseOtherValue(token);

                default:
                    throw new ArgumentException("JSON格式错误!");
            }
        }

        private object ParseObject()
        {
            index++; // 跳过 { 字符

            var obj = new Dictionary<string, object>();
            var token = GetToken();
            if (token != JsonToken.EndObject)
            {
                while (true)
                {
                    // pair<string : value>
                    if (token != JsonToken.String)
                        throw new ArgumentException("JSON格式错误!");

                    var name = ParseString();

                    if ((token = GetToken()) != JsonToken.Colon)
                        throw new ArgumentException("JSON格式错误!");

                    index++;

                    obj.Add(name, ParseValue());

                    // next token
                    if ((token = GetToken()) != JsonToken.Comma)
                        break;

                    index++;
                    token = GetToken();
                }

                if (token != JsonToken.EndObject)
                    throw new ArgumentException("JSON格式错误!");
            }

            index++; // 跳过 } 字符

            return obj;
        }

        private object ParseArray()
        {
            index++; // 跳过 [ 符号

            var array = new ArrayList();
            var token = GetToken();
            if (token != JsonToken.EndArray)
            {
                while (true)
                {
                    array.Add(ParseValue());

                    // next token
                    if ((token = GetToken()) != JsonToken.Comma)
                        break;
                    index++;
                }

                if (token != JsonToken.EndArray)
                    throw new ArgumentException("JSON格式错误!");
            }

            index++; // 跳过 ] 符号
            return array;
        }

        private double ParseNumber()
        {
            fixed (char* p = json)
            {
                int len, start = index;
                char* pStart = p + index;
                char* pCur = p + index;

                // 简单读取， 完整的数值类型还需要考虑科学记数法。
                for (; *pCur != '\0' && (IsNumberChar(*pCur) || *pCur == '.' || *pCur == '-');
                    pCur++)
                    ;

                len = (int)(pCur - pStart);
                index += len;
                return double.Parse(new string(p, start, len));
            }
        }

        private string ParseString()
        {
            index++;

            fixed (char* p = json)
            {
                char* pStart = p + index;
                char* pCur = p + index;

                buffer.Clear();
                for (; *pCur != '\0'; pCur++)
                {
                    if (*pCur == '\\')
                    {
                        char escape;
                        pCur = Escape(pCur, out escape);
                        if (escape != '\0')
                            buffer.Append(escape);

                        continue;
                    }

                    if (*pCur == '"')
                        break;

                    buffer.Append(*pCur);
                }

                index += (int)(pCur - pStart) + 1;
                return buffer.ToString();
            }
        }

        private object ParseOtherValue(JsonToken token)
        {
            fixed (char* p = json)
            {
                switch (token)
                {
                    case JsonToken.True:
                        Assert(string.CompareOrdinal("true", new string(p, index, 4)) != 0, "JSON格式错误!");
                        index += 4;
                        return true;

                    case JsonToken.False:
                        Assert(string.CompareOrdinal("false", new string(p, index, 5)) != 0, "JSON格式错误!");
                        index += 5;
                        return false;

                    case JsonToken.Null:
                        Assert(string.CompareOrdinal("null", new string(p, index, 4)) != 0, "JSON格式错误!");
                        index += 4;
                        return null;

                    default:
                        throw new ArgumentException("参数错误!");
                }
            }
        }

        private static bool IsNumberChar(char ch)
        {
            return (ch - '0' >= 0) && (ch - '0' <= 9);
        }

        private static bool IsWhiteSpace(char ch)
        {
            return ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n';
        }

        private static void Assert(bool check, string message)
        {
            if (check)
                throw new ArgumentException(message);
        }

        private static char* Escape(char* p, out char ch)
        {
            ch = '\0';
            switch (*(++p))
            {
                case '"':
                case '\\':
                case '/':
                    ch = *p;
                    break;

                case 'b':
                    ch = '\b';
                    break;

                case 'f':
                    ch = '\f';
                    break;

                case 'n':
                    ch = '\n';
                    break;

                case 'r':
                    ch = '\r';
                    break;

                case 't':
                    ch = '\t';
                    break;

                case 'u':
                    ch = Unicode(++p);
                    p += 3;
                    break;
            }

            return p;
        }

        private static char Unicode(char* p)
        {
            return (char)int.Parse(new string(p, 0, 4), NumberStyles.HexNumber);
        }
    }
}