using EleWise.ELMA.Model.Entities;
using Iesi.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using TestMocksGenerator.Models;
using TestMocksGenerator.Models.Const;

namespace TestMocksGenerator
{
    public class MockHelper
    {
        private static int _deepCounter = 0;
        public delegate Data Retriever(PropertyInfo prop, Item item);

        public MockHelper()
        {
            _deepCounter = 0;
        }

        /// <summary>
        /// Вернет словарь системных параметров
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public static Dictionary<string, string> GetSysParams(params string[] keys)
        {
            var query = string.Join(" OR ", keys.Select(key => $"Key = '{key}'"));

            var data = WebHelper.GetEntityWithQuery(EntityTypes.mcdsoft_syst_params, query);

            return FillSysParamsObject(data);
        }

        /// <summary>
        /// Вернет список сущностей по заданному EQL
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="typeUid"></param>
        /// <param name="query"></param>
        /// <param name="firstOnly"></param>
        /// <returns></returns>
        internal static List<T> GetEntitiesQuery<T>(string typeUid, string query, bool firstOnly)
        {
            var results = WebHelper.GetEntityWithQuery(typeUid, query);

            var result = new List<T>();

            if (firstOnly)
            {
                result.Add(FillObject<T>(results.FirstOrDefault(), null, null));
                return result;
            }

            foreach (var data in results)
            {
                result.Add(FillObject<T>(data, null, null));
            }

            return result;
        }

        /// <summary>
        /// Вернет контекст БП по указанному id
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static T GetWorkflow<T>(string type, string id, List<string> requiredFields)
        {
            var data = WebHelper.GetEntity(type, id);

            return FillObject<T>(data, GetInnerEntityData, requiredFields);
        }

        /// <summary>
        /// Вернёт сущность по существующему id
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static T GetEntity<T>(string type, string id)
        {
            var data = WebHelper.GetEntity(type, id);

            return FillObject<T>(data);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Data GetEntityData(string type, string id)
        {
            return WebHelper.GetEntity(type, id);
        }

        public static string GetFileDir(string fileName)
        {
            return Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName + @"\TestResponses\" + fileName;
        }

        /// <summary>
        /// Флекс
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static T FillObject<T>(Data source, Retriever retriever = null, List<string> requiredFields = null)
        {
            var result = (T)Activator.CreateInstance(typeof(T));

            _deepCounter++;

            if (_deepCounter > 3 || source == null)
            {
                _deepCounter--;
                return result;
            }               

            var ignore = new[] {
                "TypeUid",
                "UID",
                "CreationAuthor",
                "ChangeAuthor",
                "Parent",
                "ReferenceOnEntity"
            };

            foreach (var prop in result.GetType().GetProperties().Where(x => x.PropertyType.Name != "ISet`1" && !ignore.Contains(x.Name)))
            {
                try
                {
                    var dataProp = source.Items.FirstOrDefault(x => x.Name == prop.Name);

                    if (dataProp == null || (string.IsNullOrEmpty(dataProp.Value) && dataProp.Data == null && !dataProp.DataArray.Any()))
                        continue;

                    if ((prop.PropertyType is IEntity || prop.PropertyType.FullName.Contains(".CRM.Model") || prop.PropertyType.FullName.Contains(".Security.Model") || prop.PropertyType.FullName.Contains(".ConfigurationModel")) && dataProp.Data != null && !prop.PropertyType.IsInterface)
                    {
                        var innerEntityData = dataProp.Data;

                        if (retriever != null && requiredFields.Contains(prop.Name))
                        {
                            innerEntityData = retriever(prop, dataProp);
                        }

                        var method = typeof(MockHelper).GetMethod(nameof(MockHelper.FillObject));
                        var generic = method.MakeGenericMethod(prop.PropertyType);

                        var genericResult = generic.Invoke(null, new[] { innerEntityData, (object)retriever, requiredFields });

                        if (genericResult != null)
                            prop.SetValue(result, genericResult, null);

                        continue;
                    }

                    if (!string.IsNullOrEmpty(dataProp.Value))
                    {
                        prop.SetValue(result, GetCorrectTypeValue(prop.PropertyType, dataProp.Value), null);
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    Debug.Write($"Произошла ошибка! {ex.Message}. {ex.StackTrace}");
                }
            }

            foreach (var prop in result.GetType().GetProperties().Where(x => x.PropertyType.Name == "ISet`1"))
            {
                try
                {
                    var dataProp = source.Items.FirstOrDefault(x => x.Name == prop.Name);

                    if (dataProp == null || (string.IsNullOrEmpty(dataProp.Value) && dataProp.Data == null && !dataProp.DataArray.Any()))
                        continue;

                    var genericHashedSet = typeof(Iesi.Collections.Generic.HashedSet<>);

                    var typeArg = prop.PropertyType.GenericTypeArguments.FirstOrDefault();
                    var genericType = genericHashedSet.MakeGenericType(new Type[] { prop.PropertyType.GenericTypeArguments.FirstOrDefault() });
                    var list = (ISet)Activator.CreateInstance(genericType);

                    foreach (var data in dataProp.DataArray)
                    {
                        var method = typeof(MockHelper).GetMethod(nameof(MockHelper.FillObject));
                        var generic = method.MakeGenericMethod(typeArg);

                        var genericResult = generic.Invoke(null, new[] { data, null, null });

                        if (genericResult != null)
                            list.Add(genericResult);
                    }

                    prop.SetValue(result, list, null);
                }
                catch (Exception ex)
                {
                    Debug.Write($"Произошла ошибка при обработке листа! {ex.Message}. {ex.StackTrace}");
                }
            }

            _deepCounter--;

            return result;
        }

        private static Data GetInnerEntityData(System.Reflection.PropertyInfo prop, Item dataProp)
        {
            if (string.IsNullOrEmpty(dataProp.Name) || dataProp.Data == null)
                return null;

            var id = dataProp.Data.Items.FirstOrDefault(x => x.Name == "Id")?.Value;
            var typeUid = dataProp.Data.Items.FirstOrDefault(x => x.Name == "TypeUid")?.Value;

            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(typeUid))
                return null;

            var method = typeof(MockHelper).GetMethod(nameof(MockHelper.GetEntityData));
            var genericResult = (Data)method.Invoke(null, new[] { typeUid, id });

            return genericResult;
        }

        private static object GetCorrectTypeValue(Type type, string value)
        {
            try
            {
                if (type.IsEnum)
                {
                    return Enum.ToObject(type, Convert.ToInt32(value));
                }

                switch (type.Name)
                {
                    case nameof(Guid):

                        var isGuid = Guid.TryParse(value, out var guid);
                        if (isGuid)
                            return guid;

                        return Guid.Empty;
                    case nameof(Double):

                        var isDouble = Double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var dou);
                        if (isDouble)
                            return dou;

                        return default(double);
                    case nameof(Int64):

                        var isNum = Int64.TryParse(value, out var num);
                        if (isNum)
                            return num;

                        return default(Int64);
                    case nameof(DateTime):

                        var isDate = DateTime.TryParse(value, new CultureInfo("en-US", false), DateTimeStyles.None, out var date);
                        if (isDate)
                            return date;

                        return DateTime.Now;
                    case nameof(Boolean):

                        var isBoolean = Boolean.TryParse(value, out var boo);
                        if (isBoolean)
                            return boo;

                        return false;
                    case nameof(Nullable):
                    case "Nullable`1":

                        return GetCorrectTypeValue(type.GetGenericArguments().FirstOrDefault(), value);
                    case nameof(EleWise.ELMA.Model.Common.DropDownItem):

                        return new EleWise.ELMA.Model.Common.DropDownItem { Value = value };
                    default:
                        return value;
                }
            }
            catch (Exception ex)
            {
                Debug.Write($"Произошла ошибка при обработке типа type = {type.Name}.! {ex.Message}. {ex.StackTrace}");
                return null;
            }
        }

        private static Dictionary<string, string> FillSysParamsObject(Data[] source)
        {
            var result = new Dictionary<string, string>();

            foreach (var data in source)
            {
                var key = data.Items.FirstOrDefault(x => x.Name == "Key").Value;
                var value = data.Items.FirstOrDefault(x => x.Name == "Value").Value;

                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                    continue;

                result.Add(key, value);
            }

            return result;
        }
    }
}
