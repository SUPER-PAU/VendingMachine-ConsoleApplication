using System;
using Application.Lib;
using System.Linq;
using System.Collections.Generic;

namespace Application
{
    class Program
    {
        private static bool AdminMode { get; set;} = false;
        
        private static int UserMoney { get; set; } = 0;

        public static void Main(string[] args)
        {
            Console.WriteLine("Hello! To see command list type 'help' or 'h'");
            AtmDatabase.LoadProducts();

            while (true)
            {
                var input = Console.ReadLine();
                if (string.IsNullOrEmpty(input)) continue;
                
                var parts = input.Trim().Split();
                var cmd = parts[0].ToLower();
                var arg = parts.Length > 1 ? parts[1].Trim() : string.Empty;
                var additionalArg1 = parts.Length > 2 ? parts[2].Trim() : string.Empty;
                var additionalArg2 = parts.Length > 3 ? parts[3].Trim() : string.Empty;

                switch (cmd)
                {
                    case "list":
                        ShowProductsList();
                        break;
                    
                    case "insert":
                        int denominal = CheckInteger(arg);
                        int amount = CheckInteger(additionalArg1);
                        if (denominal <= 0 || amount <= 0) break;
                        
                        InsertMoney(denominal, amount);
                        break;
                    
                    case "buy":
                        Buy(arg);
                        break;
                    
                    case "cashback":
                        ReturnCashback();
                        break;
                    
                    case "balance":
                        Console.WriteLine($"Your current balance is: {UserMoney}p.");
                        break;
                    
                    case "admin":
                        if (AdminMode)
                        { Console.Error.WriteLine("Administrator mode is already enabled."); break; }
                        
                        Console.WriteLine("Administrator mode enabled. Use 'help' to see new commands.");
                        AdminMode = true;
                        break;
                    
                    case "totalbalance":
                        if (!AdminMode)
                        { Console.Error.WriteLine("Administrator mode required."); break; }
                        
                        Console.WriteLine("Denominal \t Amount");
                        PrintAllMoney();
                        break;
                    
                    case "getincome":
                        if (!AdminMode)
                        { Console.Error.WriteLine("Administrator mode required."); break ; }
                        
                        int income = CheckInteger(arg);
                        if (income <= 0) break;
                        
                        TakeMoney(income);
                        break;
                    
                    case "add":
                        if (!AdminMode)
                        { Console.Error.WriteLine("Administrator mode required."); break; }

                        if (arg == "")
                        { Console.Error.WriteLine("Invalid product Name."); break; }

                        int price = CheckInteger(additionalArg1);
                        int stock = CheckInteger(additionalArg2);
                        if (price <= 0 || stock <= 0) break;
                        
                        AddProduct(arg, price, stock);
                        break;
                    
                    case "remove":
                        if (!AdminMode)
                        { Console.Error.WriteLine("Administrator mode required."); break; }

                        if (arg == "")
                        { Console.Error.WriteLine("Invalid arguments."); break; }

                        if (additionalArg1 == "")
                            additionalArg1 = "0";

                        if (!int.TryParse(additionalArg1, out int removeCount) || removeCount < 0)
                        { Console.Error.WriteLine("Remove count must be a positive integer."); break; }

                        RemoveProduct(arg, removeCount);
                        break;
                        
                    case "user":
                        if (!AdminMode)
                        { Console.Error.WriteLine("User mode is already enabled."); break; }
                        
                        Console.WriteLine("Administrator mode disabled.");
                        AdminMode = false;
                        break;
                    
                    case "exit":
                        return;
                    
                    case "h":
                    case "help":
                        ShowHelp();
                        break;
                    
                    case "clear":
                        Console.Clear();
                        break;
                    
                    default:
                        Console.WriteLine("Please enter a valid input. To see command list type 'help'.");
                        break;
                }
            }
        }

        private static int CheckInteger(string arg)
        // проверка на то, является ли аргумент положительным числом
        {
            if (arg == "")
            { Console.Error.WriteLine("Invalid arguments."); return 0; }

            if (int.TryParse(arg, out int result) && result > 0) return result;
            Console.Error.WriteLine("Arguments must be positive integers."); return 0;
        }

        private static void Buy(string name)
        // Покупает продукт по имени, отнимая userMoney и ProductQuantity
        {
            var existing = AtmDatabase.productsList.FirstOrDefault(p =>
                string.Equals(p.productName.ToLower(), name.ToLower()));
            
            if (existing == null)
            { Console.Error.WriteLine("Product not found. Please enter a valid name."); return; }
            if (UserMoney < existing.productPrice)
            { Console.Error.WriteLine("You do not have enough money to buy this product. " +
                                      "Use insert 'denominal' 'amount' to refill balance"); return; }
            
            existing.productQuantity -= 1;
            UserMoney -= existing.productPrice;
            AtmDatabase.Save();
            Console.WriteLine($"Product bought. Current balance: {UserMoney}p.");
        }

        private static void ReturnCashback()
        // Выводит все деньги пользователя если возможно
        {
            var returned = 0;
            if (UserMoney <= 0)
            {
                Console.Error.WriteLine("Your current balance is 0p.");
                return;
            }
            foreach (var x in AtmDatabase.money.Reverse())
            {
                if (UserMoney <= 0)
                    break;
                while (x.Key <= UserMoney && AtmDatabase.money[x.Key] > 0)
                {
                    UserMoney -= x.Key;
                    AtmDatabase.money[x.Key]--;
                    returned += x.Key;
                }
            }
            AtmDatabase.Save();
            Console.WriteLine(UserMoney > 0 ? "Cannot return all cash back: the ATM is empty." +
                                              $" Returned: {returned}p. \nYour current balance is {UserMoney}p."
                                              :
                                              $"Successfully returned {returned}p.\n" +
                                              $" Your current Balance is {UserMoney}p.");
        }
        
        static void TakeMoney(int amount)
        // Выводит определенное количество денег из машины
        {
            var returned = 0;
            foreach (var x in AtmDatabase.money.Reverse())
            {
                if (amount <= 0) break;
                while (x.Key <= amount && AtmDatabase.money[x.Key] > 0)
                {
                    amount -= x.Key;
                    AtmDatabase.money[x.Key]--;
                    returned += x.Key;
                }
            }
            AtmDatabase.Save();
            Console.WriteLine(amount == 0 ? $"Successfully extracted: {returned}p."
                                : $"Cannot fully extract requested amount. Extracted: {returned}p." + 
                                  $"\n Remainder: {amount}p.");
        }

        private static void InsertMoney(int denominal, int amount)
        // принимает деньги разных номиналов и пополняет баланс пользователя
        {
            if (denominal <= 0 || amount <= 0) return;
            
            if (!AtmDatabase.money.ContainsKey(denominal)) { Console.Error.WriteLine("Invalid denomination. Available denominates:\n" + 
                "1p, 2p, 5p, 10p, 25p, 50p, 100p, 1000p."); return; }
            
            UserMoney += amount * denominal;
            AtmDatabase.money[denominal] += amount;
            Console.WriteLine($"{amount} banknotes were deposited, with a denomination of {denominal}p.");
            Console.WriteLine($"Current Balance: {UserMoney}p.");
            AtmDatabase.Save();
        }

        private static void AddProduct(string productName, int productStock, int productPrice = 1)
        // Добавляет новый продукт
        {
            if (string.IsNullOrEmpty(productName) || productPrice <= 0 || productStock <= 0) return;

            // проверка на наличичие продукта в базе
            var existing = AtmDatabase.productsList.FirstOrDefault(p => 
                string.Equals(p.productName.ToLower(), productName.ToLower()));
            // продукт существует:
            if (existing != null)
            {
                existing.productQuantity += productStock;
                // existing.productPrice = productPrice;

                AtmDatabase.Save();
                
                Console.WriteLine($"Successfully added {productStock} {productName} for {existing.productPrice}p.");
                return;
            }

            // создаем новый продукт, если такого еще не было, с новым Id
            int newId = AtmDatabase.productsList.Any() ? AtmDatabase.productsList.Max(p => p.productId) + 1 : 1;
            var newProduct = new Product
            {
                productId = newId,
                productName = productName,
                productPrice = productPrice,
                productQuantity = productStock
            };

            AtmDatabase.productsList.Add(newProduct);
            AtmDatabase.Save();
            Console.WriteLine($"Successfully added x{productStock} {productName} for {productPrice}p.");
        }

        private static void RemoveProduct(string productName, int amount)
        // Удаляет продукт по имени. Чтобы удалить определенное количество нужно указать amount
        {
            if (string.IsNullOrEmpty(productName)) { Console.Error.WriteLine( "Invalid Product name" ); return;}
            
            var existing = AtmDatabase.productsList.FirstOrDefault(p => 
                string.Equals(p.productName.ToLower(), productName.ToLower()));
            
            if (existing == null) { Console.Error.WriteLine( "Product not found. Please Enter a valid name" ); return;}

            if (existing.productQuantity <= amount || amount == 0)
            {
                AtmDatabase.productsList.Remove(existing); 
                Console.WriteLine($"Successfully removed {existing.productName} from Machine.");
                AtmDatabase.Save();
                return;
            }
            existing.productQuantity -= amount;
            AtmDatabase.Save();
            Console.WriteLine($"Successfully removed x{amount} {existing.productName} from Machine."
                + $"Remaining {existing.productName}: {existing.productQuantity}");
        }
        
        private static void ShowProductsList()
        // Выводит список всех продуктов в наличии
        {
            Console.WriteLine("==================== Products list: ====================");

            int i = 0;
            foreach (var product in AtmDatabase.productsList)
            {
                i++;
                if (product.productQuantity == 0 && !AdminMode) continue;
                
                string line = i + ". "
                                + product.productName + " \t "
                                + product.productPrice + "p, \t x"
                                + product.productQuantity;
                
                Console.WriteLine(line);
            }
        }
        
        static void PrintAllMoney()
        // Выводит полный список банкнот всех номиналов в машине
        {
            var total = 0;
            foreach (var x in AtmDatabase.money)
            {
                Console.WriteLine(x.Key + " \t \t " + x.Value);
                total += x.Value * x.Key;
            }
            Console.WriteLine($"Total ATM balance: {total}p.");
        }

        private static void ShowHelp()
        {
            Console.WriteLine("This is a vending machine that allows you to buy groceries." +
                              " To buy a product, insert money and buy the desired product.");
            Console.WriteLine("==================== Available commands: ====================");
            Console.WriteLine("list \t \t \t \t - lists all available products");
             
            Console.WriteLine("insert 'denominal' 'amount' \t - Insert money");
            Console.WriteLine("buy 'product name'  \t \t - buy a product");
            Console.WriteLine("cashback  \t \t \t - Return cashback");
            Console.WriteLine("balance  \t \t \t - Shows the current user balance");
            
            Console.WriteLine("exit \t \t \t \t - ends program");
            
            Console.WriteLine("help  \t \t \t \t - shows this message");
            Console.WriteLine("clear  \t \t \t \t - Clear the console");
            
            if (!AdminMode) return;
            Console.WriteLine("admin  \t \t \t \t - enables administrator mode");
            
            Console.WriteLine("totalbalance  \t \t \t - Shows total ATM balance");
            Console.WriteLine("add 'name' 'amount' 'price' \t - adds an amount of product quantity to the list");
            Console.WriteLine("remove 'amount' \t \t - removes a product from the list. Use 'amount' for specific quantity.");
            Console.WriteLine("getincome 'amount' \t \t - Extracts an amount of money from ATM");
            
            Console.WriteLine("user  \t \t \t \t - enables user mode");
            
        }
    }
}