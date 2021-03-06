﻿extern alias pt;
using Enyim.Caching.Memcached;
using MemcachedTranscoder.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pt::MemcachedTranscoder;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace MemcachedTranscoder.Proto.Tests
{
    [Serializable]
    [DataContract]
    public class GenericType<T>
    {
        [DataMember(Order = 1)]
        public int MyProperty { get; set; }
        [DataMember(Order = 2)]
        public T MyProperty2 { get; set; }
    }

    [TestClass]
    public class MemcachedTest
    {
        [TestInitialize]
        public void Init()
        {
            var client = MemcachedHelper.CreateClient(new ProtoTranscoder());
            client.FlushAll();
        }

        public ConcurrentDictionary<ArraySegment<byte>, Type> GetReadCache()
        {
            var readCache = (ConcurrentDictionary<ArraySegment<byte>, Type>)(typeof(ProtoTranscoder).GetField("readCache", BindingFlags.Static | BindingFlags.GetField | BindingFlags.NonPublic).GetValue(null));
            return readCache;
        }

        public ConcurrentDictionary<Type, byte[]> GetWriteCache()
        {
            var writeCache = (ConcurrentDictionary<Type, byte[]>)(typeof(ProtoTranscoder).GetField("writeCache", BindingFlags.Static | BindingFlags.GetField | BindingFlags.NonPublic).GetValue(null));
            return writeCache;
        }

        [TestMethod]
        public void MemcachedCheck()
        {
            var client = MemcachedHelper.CreateClient(new ProtoTranscoder());
            var store = client.Store(StoreMode.Add, "hugahuga", 100);
            store.Is(true);

            var value = client.Get<int>("hugahuga");
            value.Is(100);
        }

        [TestMethod]
        public void CollectionStore()
        {
            var client = MemcachedHelper.CreateClient(new ProtoTranscoder());

            GetReadCache().Clear();
            GetWriteCache().Clear();

            client.Store(StoreMode.Add, "listKey", new List<int> { 0, 1, 2, 3 }).Is(true);
            client.Get<List<int>>("listKey").Is(0, 1, 2, 3);

            client.Store(StoreMode.Add, "arrayKey", new int[] { 1, 2, 3 }).Is(true);
            client.Get<int[]>("arrayKey").Is(1, 2, 3);

            client.Store(StoreMode.Add, "arrayKey2", new int[] { 1, 2, 3, 4, 5 }).Is(true);
            client.Get<int[]>("arrayKey2").Is(1, 2, 3, 4, 5);

            var values = client.Get(new[] { "listKey", "arrayKey", "arrayKey2" });
            (values["listKey"] as List<int>).Is(0, 1, 2, 3);
            (values["arrayKey"] as int[]).Is(1, 2, 3);
            (values["arrayKey2"] as int[]).Is(1, 2, 3, 4, 5);

            GetReadCache().Count.Is(2);
            GetWriteCache().Count.Is(2);
        }

        [TestMethod]
        public void ObjectStore()
        {
            var client = MemcachedHelper.CreateClient(new ProtoTranscoder());

            GetReadCache().Clear();
            GetWriteCache().Clear();

            client.Store(StoreMode.Add, "objKey", new MyClass { MyProperty = 10, MyProperty2 = "test" }).Is(true);
            client.Get("objKey").IsNotNull();
            (client.Get("objKey") as MyClass).Is(x => x.MyProperty == 10 && x.MyProperty2 == "test");
            client.Get<MyClass>("objKey").Is(x => x.MyProperty == 10 && x.MyProperty2 == "test");

            client.Store(StoreMode.Add, "genKey", new GenericType<int> { MyProperty = 10, MyProperty2 = 100 }).Is(true);
            client.Get<GenericType<int>>("genKey").Is(x => x.MyProperty == 10 && x.MyProperty2 == 100);

            client.Store(StoreMode.Add, "genKey2", new GenericType<string> { MyProperty = 10, MyProperty2 = "hyaku" }).Is(true);
            client.Get<GenericType<string>>("genKey2").Is(x => x.MyProperty == 10 && x.MyProperty2 == "hyaku");

            GetReadCache().Count.Is(3);
            GetWriteCache().Count.Is(3);
        }
    }
}