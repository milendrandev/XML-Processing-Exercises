using CarDealer.Data;
using CarDealer.Dtos.Import;
using CarDealer.Models;
using CarDealer.Dtos.Export;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace CarDealer
{
    public class StartUp
    {
        public static void Main(string[] args)
        {
            var context = new CarDealerContext();

            using (context)
            {
                //context.Database.EnsureDeleted();
                //context.Database.EnsureCreated();

               // var inputXml = File.ReadAllText("../../../Datasets/sales.xml");

                System.Console.WriteLine(GetSalesWithAppliedDiscount(context));
            }
        }

        //Problem 09
        public static string ImportSuppliers(CarDealerContext context, string inputXml)
        {
            var xmlSer = new XmlSerializer(typeof(SuppliersImportDto[]), new XmlRootAttribute("Suppliers"));

            var suppliersDto = (SuppliersImportDto[])xmlSer.Deserialize(new StringReader(inputXml));

            var suppliers = suppliersDto.Select(s => new Supplier
            {
                Name = s.Name,
                IsImporter = s.IsImporter
            }).ToArray();

            context.Suppliers.AddRange(suppliers);
            context.SaveChanges();

            return $"Successfully imported {suppliers.Length}";
        }

        //Problem 10
        public static string ImportParts(CarDealerContext context, string inputXml)
        {
            var xmlSer = new XmlSerializer(typeof(PartsImportDto[]), new XmlRootAttribute("Parts"));

            var partsDto = (PartsImportDto[])xmlSer.Deserialize(new StringReader(inputXml));

            var parts = partsDto
                .Where(p => p.SupplierId <= context.Suppliers.Count())
                .Select(p => new Part
                {
                    Name = p.Name,
                    Price = p.Price,
                    Quantity = p.Quantity,
                    SupplierId = p.SupplierId
                }).ToArray();

            context.Parts.AddRange(parts);
            context.SaveChanges();

            return $"Successfully imported {parts.Length}";
        }

        //Problem 11
        public static string ImportCars(CarDealerContext context, string inputXml)
        {
            var xmlSer = new XmlSerializer(typeof(CarsImportDto[]), new XmlRootAttribute("Cars"));

            var carsDto = (CarsImportDto[])xmlSer.Deserialize(new StringReader(inputXml));

            var cars = new List<Car>();
            var parts = new List<PartCar>();

            foreach (var car in carsDto)
            {
                var newCar = new Car
                {
                    Make = car.Make,
                    Model = car.Model,
                    TravelledDistance = car.TravelledDistance
                };

                cars.Add(newCar);

                var partsId = car.PartsId
                    .Where(pDto => context.Parts.Any(p => p.Id == pDto.Id))
                    .Select(p => p.Id);

                foreach (var partId in partsId.Distinct())
                {
                    var newPartCar = new PartCar
                    {
                        PartId = partId,
                        Car = newCar
                    };
                    parts.Add(newPartCar);
                }
            }

            context.Cars.AddRange(cars);
            context.PartCars.AddRange(parts);
            context.SaveChanges();

            return $"Successfully imported {cars.Count}";
        }

        //Problem 12
        public static string ImportCustomers(CarDealerContext context, string inputXml)
        {
            var xmlSer = new XmlSerializer(typeof(CustomersImportDto[]), new XmlRootAttribute("Customers"));

            var customersDto = (CustomersImportDto[])xmlSer.Deserialize(new StringReader(inputXml));

            var customers = customersDto.Select(c => new Customer
            {
                Name = c.Name,
                BirthDate = c.BirthDate,
                IsYoungDriver = c.IsYoungDriver
            }).ToArray();

            context.Customers.AddRange(customers);
            context.SaveChanges();

            return $"Successfully imported {customers.Length}";
        }

        //Problem 13
        public static string ImportSales(CarDealerContext context, string inputXml)
        {
            var xmlSer = new XmlSerializer(typeof(SalesImportDto[]), new XmlRootAttribute("Sales"));

            var salesDto = (SalesImportDto[])xmlSer.Deserialize(new StringReader(inputXml));

            var sales = salesDto
                .Where(s => s.CarId <= context.Cars.Count())
                .Select(s => new Sale
                {
                    CarId = s.CarId,
                    CustomerId = s.CustomerId,
                    Discount = s.Discount
                }).ToArray();

            context.Sales.AddRange(sales);
            context.SaveChanges();

            return $"Successfully imported {sales.Length}";
        }

        //Problem 14
        public static string GetCarsWithDistance(CarDealerContext context)
        {
            var cars = context.Cars
                .Where(c => c.TravelledDistance > 2000000)
                .Select(c => new CarWithTravelledDistanceDto
                {
                    Make = c.Make,
                    Model = c.Model,
                    TravelledDistance = c.TravelledDistance
                })
                .OrderBy(c => c.Make)
                .ThenBy(c => c.Model)
                .Take(10)
                .ToArray();

            var xmlSer = new XmlSerializer(cars.GetType(), new XmlRootAttribute("cars"));

            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", "");

            var writer = new StringWriter();

            using (writer)
            {
                xmlSer.Serialize(writer, cars,namespaces);
            }

            return writer.ToString();
        }

        //Problem 15
        public static string GetCarsFromMakeBmw(CarDealerContext context)
        {
            var carsBmw = context.Cars
                .Where(c => c.Make == "BMW")
                .Select(c => new CarModelBMWDto
                {
                    Id = c.Id,
                    Model = c.Model,
                    TravelledDistance = c.TravelledDistance,

                })
                .OrderBy(c => c.Model)
                .ThenByDescending(c => c.TravelledDistance)
                .ToArray();

            var xmlSer = new XmlSerializer(carsBmw.GetType(), new XmlRootAttribute("cars"));

            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", "");

            var writer = new StringWriter();

            using (writer)
            {
                xmlSer.Serialize(writer, carsBmw,namespaces);
            }

            return writer.ToString();
        }

        //Problem 16
        public static string GetLocalSuppliers(CarDealerContext context)
        {
            var suppliers = context.Suppliers
                .Where(s => !s.IsImporter)
                .Select(s => new ExportLocalSuppliers
                {
                    Id = s.Id,
                    Name = s.Name,
                    PartsCount = s.Parts.Count
                })
                .ToArray();

            var xmlSer = new XmlSerializer(suppliers.GetType(), new XmlRootAttribute("suppliers"));

            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", "");

            var writer = new StringWriter();

            using (writer)
            {
                xmlSer.Serialize(writer, suppliers, namespaces);
            }

            return writer.ToString();
        }

        //Problem 17
        public static string GetCarsWithTheirListOfParts(CarDealerContext context)
        {
            var cars = context.Cars
                .Select(c => new GetCarsWithPartsDto
                {
                    Make = c.Make,
                    Model = c.Model,
                    TravelledDistance = c.TravelledDistance,
                    Parts = c.PartCars.Select(p => new PartDto
                    {
                        Name = p.Part.Name,
                        Price = p.Part.Price
                    })
                    .OrderByDescending(p => p.Price)
                    .ToArray()
                })
                .OrderByDescending(c => c.TravelledDistance)
                .ThenBy(c => c.Model)
                .Take(5)
                .ToArray();

            var xmlSer = new XmlSerializer(cars.GetType(), new XmlRootAttribute("cars"));

            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", "");

            var writer = new StringWriter();

            using (writer)
            {
                xmlSer.Serialize(writer, cars,namespaces);
            }

            return writer.ToString();
        }

        //Problem 18
        public static string GetTotalSalesByCustomer(CarDealerContext context)
        {
            var customers = context.Customers
                .Where(c => c.Sales.Any())
                .Select(c => new GetTotalSalesByCustomerDto
                {
                    FullName = c.Name,
                    BoughtCars = c.Sales.Count,
                    SpentMoney = c.Sales.Sum(s => s.Car.PartCars.Sum(p => p.Part.Price))
                })
                .OrderByDescending(c=> c.SpentMoney)
                .ToArray();

            var xmlSer = new XmlSerializer(customers.GetType(), new XmlRootAttribute("customers"));

            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", "");

            var writer = new StringWriter();

            using (writer)
            {
                xmlSer.Serialize(writer, customers, namespaces);
            }

            return writer.ToString();
        }

        //Problem 19
        public static string GetSalesWithAppliedDiscount(CarDealerContext context)
        {
            var sales = context.Sales
                .Select(s => new GetSalesWithDiscountDto
                {
                    Car = new CarWithTravelledDistanceDto
                    {
                        Make = s.Car.Make,
                        Model = s.Car.Model,
                        TravelledDistance = s.Car.TravelledDistance
                    },
                    Discount = s.Discount,
                    CustomerName = s.Customer.Name,
                    Price = s.Car.PartCars.Sum(p => p.Part.Price),
                    PriceWithDiscount = s.Car.PartCars.Sum(p => p.Part.Price)
                    - ((s.Discount / 100) * s.Car.PartCars.Sum(p => p.Part.Price))
                }).ToArray();

            var xmlSer = new XmlSerializer(sales.GetType(), new XmlRootAttribute("sales"));

            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", "");

            var writer = new StringWriter();

            using (writer)
            {
                xmlSer.Serialize(writer, sales, namespaces);
            }

            return writer.ToString();
        }
    }
}