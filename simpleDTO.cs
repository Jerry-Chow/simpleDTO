/// <summary>
    /// 提供将SqlDataReader转成T类型的扩展方法
    /// </summary>
    public static class SqlDataReaderEx
    {
        private static object _obj = new object();
        /// <summary>
        /// 属性反射信息缓存 key:类型的hashCode,value属性信息
        /// </summary>
        private static Dictionary<int, Dictionary<string, PropertyInfo>> propInfoCache = new Dictionary<int, Dictionary<string, PropertyInfo>>();

        /// <summary>
        /// 将SqlDataReader转成T类型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static T To<T>(this SqlDataReader reader)
          where T : new()
        {
            if (reader == null || reader.HasRows == false) return default(T);

            var res = new T();
            var propInfos = GetFieldnameFromCache<T>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                var n = reader.GetName(i).ToLower();
                if (propInfos.ContainsKey(n))
                {
                    PropertyInfo prop = propInfos[n];
                    var IsValueType = prop.PropertyType.IsValueType;
                    object defaultValue = null;//引用类型或可空值类型的默认值
                    if (IsValueType) {
                        if ((!prop.PropertyType.IsGenericType)
                            ||(prop.PropertyType.IsGenericType&&!prop.PropertyType.GetGenericTypeDefinition().Equals(typeof(Nullable<>))))
                        {
                            defaultValue = 0;//非空值类型的默认值
                        }
                    }
                    var v = reader.GetValue(i);
                    prop.SetValue(res, (Convert.IsDBNull(v) ? defaultValue : v), null);
                }
            }

            return res;
        }

        private static Dictionary<string, PropertyInfo> GetFieldnameFromCache<T>()
        {
            Dictionary<string, PropertyInfo> res = null;
            var hashCode = typeof(T).GetHashCode();
            var filedNames = GetFieldname<T>();
            lock (_obj)
            {
                if (!propInfoCache.ContainsKey(hashCode))
                {
                    propInfoCache.Add(hashCode, filedNames);
                }
            }
            res = propInfoCache[hashCode];
            return res;
        }

        /// <summary>
        /// 获取一个类型的对应数据表的字段信息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static Dictionary<string, PropertyInfo> GetFieldname<T>()
        {
            var res = new Dictionary<string, PropertyInfo>();
            var props = typeof(T).GetProperties();
            foreach (PropertyInfo item in props)
            {                
                res.Add(item.GetFieldName(), item);
            }
            return res;
        }

     

        /// <summary>
        /// 将SqlDataReader转成List<T>类型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static List<T> ToList<T>(this SqlDataReader reader)
            where T : new()
        {
            if (reader == null || reader.HasRows == false) return null;
            var res = new List<T>();
            while (reader.Read())
            {
                res.Add(reader.To<T>());
            }
            return res;
        }  
        
        /// <summary>
        /// 获取该属性对应到数据表中的字段名称
        /// </summary>
        /// <param name="propInfo"></param>
        /// <returns></returns>
        public static string GetFieldName(this PropertyInfo propInfo)
        {
            var fieldname = propInfo.Name;
            var attr = propInfo.GetCustomAttributes(false);
            foreach (var a in attr)
            {
                if (a is DataFieldAttribute)
                {
                    fieldname = (a as DataFieldAttribute).Name;
                    break;
                }
            }
            return fieldname.ToLower();
        }
    }
