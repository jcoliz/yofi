﻿using jcoliz.OfficeOpenXml.Serializer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Repositories;

namespace YoFi.Tests.Helpers
{
    public abstract class BaseMockRepository<T>: IMockRepository<T> where T: class, IModelItem, new()
    {
        public void AddItems(int numitems) => Items.AddRange(MakeItems(numitems));

        public abstract T MakeItem(int x);

        public IEnumerable<T> MakeItems(int numitems) => Enumerable.Range(1, numitems).Select(MakeItem);

        public bool Ok { get; set; } = true;

        public List<T> Items { get; } = new List<T>();

        public IQueryable<T> All => Items.AsQueryable();

        public IQueryable<T> OrderedQuery => throw new System.NotImplementedException();

        public Task AddAsync(T item)
        {
            if (!Ok)
                throw new Exception("Failed");

            Items.Add(item);
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(IEnumerable<T> items)
        {
            if (!Ok)
                throw new Exception("Failed");

            Items.AddRange(items);
            return Task.CompletedTask;
        }

        public Stream AsSpreadsheet()
        {
            if (!Ok)
                throw new Exception("Failed");

            var items = All;

            var stream = new MemoryStream();
            using (var ssw = new SpreadsheetWriter())
            {
                ssw.Open(stream);
                ssw.Serialize(items);
            }

            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }

        public Task<T> GetByIdAsync(int? id) => Ok ? Task.FromResult(All.Single(x => x.ID == id.Value)) : throw new Exception("Failed");

        public Task RemoveAsync(T item)
        {
            if (!Ok)
                throw new Exception("Failed");

            if (item == null)
                throw new ArgumentException("Expected non-null item");

            var index = Items.FindIndex(x => x.ID == item.ID);
            Items.RemoveAt(index);

            return Task.CompletedTask;
        }

        public Task<bool> TestExistsByIdAsync(int id)
        {
            throw new System.NotImplementedException();
        }

        public Task UpdateAsync(T item)
        {
            if (!Ok)
                throw new Exception("Failed");

            if (item == null)
                throw new ArgumentException("Expected non-null item");

            var index = Items.FindIndex(x => x.ID == item.ID);
            Items[index] = item;

            return Task.CompletedTask;
        }

        public IQueryable<T> InDefaultOrder(IQueryable<T> original) => original;

        public abstract IQueryable<T> ForQuery(string q);
    }
}