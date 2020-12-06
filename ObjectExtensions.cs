using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;

namespace DumpVisualizer
{
    public static class ObjectExtensions
    {
        private static int tableIndex = 1;
        public static string Dump(this object source, int maxDepth = 5, int count = 1000)
        {
            tableIndex = 1;
            var builder = new StringBuilder();
            FillObject(builder, source, maxDepth, count);
            return GetHeader() + builder.ToString() + GetFooter();
        }

        private static string GetSystemObjectAsString(object source, int maxDepth)
        {
            if (maxDepth == 0 || !source.GetType().IsArray)
                return source is string s ? "\"" + s + "\"" : source.ToString();
            var array = (Array)source;
            var parts = new List<string>();
            foreach (var part in array)
            {
                parts.Add(GetSystemObjectAsString(part, maxDepth - 1));
            }
            return "[" + string.Join(",", parts) + "]";
        }

        private static void FillObject(StringBuilder builder, object source, int maxDepth, int count, Type realType = null)
        {
            if (source == null)
            {
                builder.AppendLine("<i>null</i>");
            }
            else
            {
                var type = source.GetType();
                realType = realType ?? type;
                if (maxDepth == 0)
                {
                    builder.AddSafe(source.ToString());
                }
                else if (IsSimple(type))
                {
                    builder.AddSafe(GetSystemObjectAsString(source, maxDepth));
                }

                #region Special Types

                #region Dictionary

                else if (source is IDictionary)
                {
                    using (AddType(builder, realType, 2))
                    {
                        using (new BuilderTag(builder, $"tr"))
                        {
                            using (new BuilderTag(builder, $"th"))
                                builder.AddSafe("Key");
                            using (new BuilderTag(builder, $"th"))
                                builder.AddSafe("Value");
                        }
                        var rowCount = 0;
                        foreach (DictionaryEntry entry in source as IDictionary)
                        {
                            rowCount++;
                            if (rowCount > count)
                                break;
                            using (new BuilderTag(builder, $"tr"))
                            {
                                using (new BuilderTag(builder, $"td"))
                                    FillObject(builder, entry.Key, maxDepth - 1, count);
                                using (new BuilderTag(builder, $"td"))
                                    FillObject(builder, entry.Value, maxDepth - 1, count);                                
                            }
                        }
                    }                    
                }

                #endregion

                #region Data

                #region DataTable

                else if (typeof(DataTable).IsAssignableFrom(type))
                {
                    var table = (DataTable)source;
                    var columnCount = table.Columns.Count;
                    var format = "{0} - [" + table.Rows.Count + " Rows]";
                    using (AddType(builder, realType, columnCount, format))
                    {
                        var colNames = new List<string>();
                        foreach (DataColumn column in table.Columns)
                        {
                            colNames.Add(column.ColumnName);
                        }
                        AddProperties(builder, colNames);
                        var rowCount = 0;
                        foreach (DataRow row in table.Rows)
                        {
                            rowCount++;
                            if (rowCount > count)
                                break;
                            using (new BuilderTag(builder, $"tr"))
                            {
                                for (var columnIndex = 0; columnIndex < columnCount; columnIndex++)
                                {
                                    var value = row[columnIndex];
                                    using (new BuilderTag(builder, $"td"))
                                        FillObject(builder, value, maxDepth - 1, count);
                                }
                            }
                        }
                    }                    
                }

                #endregion

                #region IDataReader

                else if (typeof(IDataReader).IsAssignableFrom(type))
                {
                    var reader = (IDataReader)source;
                    var table = new DataTable();
                    table.Load(reader);
                    FillObject(builder, table, maxDepth, count, type);
                }

                #endregion

                #endregion

                #region IEnumerator

                else if (GetEnumInterface(type) != null)
                {
                    var enumType = GetEnumInterface(type);
                    var properties = GetAllProperties(enumType);
                    var format = "{0}";
                    var collection = source as ICollection;
                    if (collection != null)
                    {
                        format = "{0} - [" + collection.Count + " Rows]";
                    }
                    using (AddType(builder, realType, properties.Count, format))
                    {
                        AddProperties(builder, properties.Select(propInfo => propInfo.Name));
                        var rowCount = 0;
                        foreach (var item in source as IEnumerable)
                        {
                            rowCount++;
                            if (rowCount > count)
                                break;
                            using (new BuilderTag(builder, $"tr"))
                            {
                                if (IsSimple(enumType) || GetEnumInterface(enumType) != null)
                                {
                                    var value = item;
                                    using (new BuilderTag(builder, $"td"))
                                        FillObject(builder, value, maxDepth - 1, count);
                                }
                                else
                                {
                                    foreach (var propInfo in properties)
                                    {
                                        var value = propInfo.GetValue(item);
                                        using (new BuilderTag(builder, $"td"))
                                            FillObject(builder, value, maxDepth - 1, count);
                                    }
                                }
                            }
                        }
                    }                    
                }

                #endregion

                #endregion

                else
                {
                    using (AddType(builder, realType, 2))
                    {
                        foreach (var propInfo in GetAllProperties(type))
                        {
                            AddProperty(builder, propInfo, source, maxDepth - 1, count);
                        }
                    }                    
                }
            }
        }

        private static bool IsSimple(Type type)
        {
            if (type.IsArray)
            {
                return IsSimple(type.GetElementType());
            }
            return type.IsPrimitive 
                || type == typeof(string) || type == typeof(StringBuilder) || type == typeof(Uri) 
                || type == typeof(DateTime) || type == typeof(TimeSpan) || type == typeof(DateTimeOffset)
                || type.IsEnum;
        }

        private static void AddProperties(StringBuilder builder, IEnumerable<string> properties)
        {
            if (properties.Any())
            {
                using (new BuilderTag(builder, "tr"))
                {
                    foreach (var propName in properties)
                    {
                        using (new BuilderTag(builder, "th"))
                            builder.AddSafe(propName);
                    }
                }
            }
        }

        private static List<PropInfo> GetAllProperties(Type type)
        {
            if (IsSimple(type) || GetEnumInterface(type) != null)
                return new List<PropInfo>();
            var result = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .Select(p => new PropInfo(p))
                .ToList();
            if (result.Count == 0) //Types like ValueTuple don't have properties, but work on fields directly
            {
                result = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                    .Select(f => new PropInfo(f))
                    .ToList();
            }
            var toStringMethod = type.GetMethod("ToString", new Type[0]);
            if (toStringMethod != null && toStringMethod.DeclaringType != typeof(object) &&  //ToString has a specific implementation. Could provide usefull data
                !toStringMethod.DeclaringType.Namespace.StartsWith("System")) //Ignores default ToString implemetation, like ValueTuple
                result.Insert(0, new PropInfo());
            return result;
        }

        private static void AddProperty(StringBuilder builder, PropInfo propInfo, object source, int maxDepth, int count)
        {
            var value = propInfo.GetValue(source);
            using (new BuilderTag(builder, $"tr"))
            {
                using (new BuilderTag(builder, $"th class=\"member\""))
                    builder.AddSafe(propInfo.Name);
                using (new BuilderTag(builder, $"td"))
                    FillObject(builder, value, maxDepth, count);
            }
        }

        private static IDisposable AddType(StringBuilder builder, Type type, int colCount, string format = null)
        {
            format = format ?? "{0}";
            var result = new BuilderTag(builder, $"table id=\"t{tableIndex}\"");
            using (new BuilderTag(builder, "tr"))
            {
                builder.AppendLine($"<td class=\"typeheader\" colspan=\"{colCount}\"><a href=\"\" class=\"typeheader\" onclick=\"return toggle('t{tableIndex}');\"><span class=\"typeglyph\" id=\"t{tableIndex}ud\">5</span>");
                builder.AddSafe(string.Format(format, type.ToString()));
                builder.AppendLine($"</a><a href=\"\" class=\"fixedextenser\" onclick=\"return window.external.CustomClick('0','t{tableIndex}','',false);\"><span class=\"fixedextenser\">4</span></a></td>");
                tableIndex++;
            }
            return result;
        }

        private static Type GetEnumInterface(Type type)
        {
            var interfaces = type.GetInterfaces();
            var enumInterface = interfaces.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            return enumInterface?.GetGenericArguments()[0];
        }

        private static void AddSafe(this StringBuilder builder, string content)
        {
            builder.AppendLine(WebUtility.HtmlEncode(content));
        }

        private static string GetHeader()
        {
            var start = @"<!DOCTYPE HTML []>
<html>
<head>
<meta http-equiv=""Content-Type"" content=""text/html;charset=utf-8"" />
<meta http-equiv=""X-UA-Compatible"" content=""IE=edge"" />
<meta name=""Generator"" content=""github.com/hyperlinq/hyperlinq"" />
<style type=""text/css"">
body{margin:0.3em 0.3em 0.4em 0.4em;font-family:Verdana;font-size:80%;background:white}p,pre{margin:0;padding:0;font-family:Verdana}table{border-collapse:collapse;border:2px solid #17b;margin:0.3em 0.2em}table.limit{border-bottom-color:#c31}table.expandable{border-bottom-style:dashed}table.error{border-bottom-width:4px}td,th{vertical-align:top;border:1px solid #aaa;padding:0.1em 0.2em;margin:0}th{text-align:left;background-color:#ddd;border:1px solid #777;font-family:tahoma;font-size:.9em;font-weight:bold}th.member{padding:0.1em 0.2em 0.1em 0.2em}td.typeheader{font-family:tahoma;font-weight:bold;background-color:#17b;color:white;padding:0 0.2em 0.15em 0.1em}td.n{text-align:right}a:link.typeheader,a:visited.typeheader,a:link.extenser,a:visited.extenser,a:link.fixedextenser,a:visited.fixedextenser{font-family:tahoma;font-size:.9em;font-weight:bold;text-decoration:none;background-color:#17b;color:white;float:left}a:link.difheader,a:visited.difheader{color:#ff8}a:link.extenser,a:visited.extenser,a:link.fixedextenser,a:visited.fixedextenser{float:right;padding-left:2pt;margin-left:4pt}span.typeglyph,span.typeglyphx{padding:0 0.2em 0 0;margin:0}span.extenser,span.extenserx,span.fixedextenser{margin-top:1.2pt}span.typeglyph,span.extenser,span.fixedextenser{font-family:webdings}span.fixedextenser{display:none;position:fixed;right:6px}td.typeheader:hover .fixedextenser{display:block}span.typeglyphx,span.extenserx{font-family:arial;font-weight:bold;margin:2px}table.group{border:none;margin:0}td.group{border:none;padding:0 0.1em}div.spacer{margin:0.6em 0}table.headingpresenter{border:none;border-left:3px dotted #1a5;margin:.8em 0 1em 0.15em}th.headingpresenter{font-family:Arial;border:none;padding:0 0 0.2em 0.5em;background-color:white;color:green;font-size:1.1em}td.headingpresenter{border:none;padding:0 0 0 0.6em}td.summary{background-color:#def;color:#024;font-family:Tahoma;padding:0 0.1em 0.1em 0.1em}td.columntotal{font-family:Tahoma;background-color:#eee;font-weight:bold;color:#17b;font-size:.9em;text-align:right}span.graphbar{background:#17b;color:#17b;margin-left:-2px;margin-right:-2px}a:link.graphcolumn,a:visited.graphcolumn{color:#17b;text-decoration:none;font-weight:bold;font-family:Arial;font-size:1.1em;letter-spacing:-0.2em;margin-left:0.1em;margin-right:0.2em}a:link.collection,a:visited.collection{color:green}a:link.reference,a:visited.reference{color:blue}i{color:green}em{color:red}.highlight{background:#ff8}.fixedfont{font-family:Consolas,monospace}code{font-family:Consolas}code.xml b{color:blue;font-weight:normal}code.xml i{color:maroon;font-weight:normal;font-style:normal}code.xml em{color:red;font-weight:normal;font-style:normal}span.cc{background:#666;color:white;margin:0 1.5px;padding:0 1px;font-family:Consolas,monospace;border-radius:3px}.difadd{background:#d3f3d3}.difremove{background:#f3d8d8}::-ms-clear{display:none}input,textarea,button,select{font-family:Verdana;font-size:1em;padding:.2em}button{padding:.2em .4em}input,textarea,select{margin:.15em 0}input[type=""checkbox""],input[type=""radio""]{margin:0 0.4em 0 0;height:0.9em;width:0.9em}input[type=""radio""]:focus,input[type=""checkbox""]:focus{outline:thin dotted red}.checkbox-label{vertical-align:middle;position:relative;bottom:.07em;margin-right:.5em}fieldset{margin:0 .2em .4em .1em;border:1pt solid #aaa;padding:.1em .6em .4em .6em}legend{padding:.2em .1em}
</style>";
            var scripts = @"<script language='JavaScript' type='text/javascript'>
function toggle(id) {
table=document.getElementById(id); if (table==null) return false;
updown=document.getElementById(id+'ud'); if (updown==null) return false;
if (updown.innerHTML=='5'||updown.innerHTML=='6') { expand=updown.innerHTML=='6'; updown.innerHTML=expand?'5':'6'; } else { expand=updown.innerHTML=='˅'; updown.innerHTML=expand?'˄':'˅'; }
table.style.borderBottomStyle=expand?'solid':'dashed'; elements=table.rows; if(elements.length== 0||elements.length==1) return false;
for (i=1;i!=elements.length;i++) if (elements[i].id.substring(0,3)!='sum') elements[i].style.display = expand?'table-row':'none';
return false;
}
</script>";
            var end = @"</head>
<body>";
            return start + scripts + end;
        }
        private static string GetFooter() => "</body></html>";

        private class PropInfo
        {
            private readonly PropertyInfo _propertyInfo;
            private readonly FieldInfo _fieldInfo;
            public PropInfo(PropertyInfo propertyInfo)
            {
                _propertyInfo = propertyInfo;
            }

            public PropInfo(FieldInfo fieldInfo)
            {
                _fieldInfo = fieldInfo;
            }

            public PropInfo()
            { }

            public string Name => _propertyInfo?.Name ?? _fieldInfo?.Name ?? "String Value";
            public object GetValue(object obj) => _propertyInfo?.GetValue(obj) ?? _fieldInfo?.GetValue(obj) ?? obj.ToString();
        }

        private class BuilderTag: IDisposable
        {
            private readonly StringBuilder _builder;
            private readonly string _tag;

            public BuilderTag(StringBuilder builder, string tag)
            {
                _builder = builder;
                _builder.AppendLine($"<{tag}>");
                var i = tag.IndexOf(' ');                
                _tag = i < 0 ? tag: tag.Substring(0, i);
            }

            public void Dispose() 
            {
                _builder.AppendLine($"</{_tag}>");
            }

        }

    }
}
