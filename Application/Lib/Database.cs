using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Application.Lib
{
    // Сделал static, т.к. не нужно несколько экземпляров для одной базы
    public static class AtmDatabase
    {
        public static List<Product> productsList { get; set; }
        public static SortedDictionary<int, int> money = new SortedDictionary<int, int>();
        
        private const string SavePath = "VendingMachineSave.json";

        public static void Save()
        {
            var wrapper = new SaveWrapper()
            {
                ProductsSaveList = productsList,
                AtmSaveBank = money
            };
            var json = JsonConvert.SerializeObject(wrapper, Formatting.Indented);
            File.WriteAllText(SavePath, json);
        }

        public static void LoadProducts()
        {
            if (!File.Exists(SavePath))
            {
                // Создам новый список продуктов, если нет текущего
                productsList = new List<Product>
                {
                    new Product { productId = 1, productName = "Чипсы", productPrice = 123, productQuantity = 2 },
                    new Product { productId = 2, productName = "Вода", productPrice = 43, productQuantity = 1 },
                    new Product { productId = 3, productName = "Кола", productPrice = 87, productQuantity = 1 }
                };
                money = new SortedDictionary<int, int>
                {
                    { 1, 12 },
                    { 2, 12 },
                    { 5, 9 },
                    { 10, 6 },
                    { 25, 4 },
                    { 50, 3 },
                    { 100, 2 },
                    { 500, 1 },
                    { 1000, 1 }
                };
                Save();
                return;
            }

            string json = File.ReadAllText(SavePath);
            productsList = JsonConvert.DeserializeObject<SaveWrapper>(json).ProductsSaveList;
            money = JsonConvert.DeserializeObject<SaveWrapper>(json).AtmSaveBank;
        }
    }
    
    
    [System.Serializable]
    class SaveWrapper
    {
        public List<Product> ProductsSaveList { get; set; }
        public SortedDictionary<int, int> AtmSaveBank { get; set; }
    }
}

