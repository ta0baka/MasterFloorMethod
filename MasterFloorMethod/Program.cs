using Npgsql;

namespace MaterialCalculatorConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = "Host=localhost;Username=postgres;Password=****;Database=master_floor";

            Console.WriteLine("Калькулятор необходимого количества материала");

            try
            {
                Console.Write("Введите ID типа продукции: ");
                string? productTypeIdInput = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(productTypeIdInput))
                {
                    Console.WriteLine("Ошибка: Не введен ID типа продукции");
                    return;
                }
                int productTypeId = int.Parse(productTypeIdInput);

                Console.Write("Введите ID типа материала: ");
                string? materialTypeIdInput = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(materialTypeIdInput))
                {
                    Console.WriteLine("Ошибка: Не введен ID типа материала");
                    return;
                }
                int materialTypeId = int.Parse(materialTypeIdInput);

                Console.Write("Введите количество продукции: ");
                string? productQuantityInput = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(productQuantityInput))
                {
                    Console.WriteLine("Ошибка: Не введено количество продукции");
                    return;
                }
                int productQuantity = int.Parse(productQuantityInput);

                Console.Write("Введите первый параметр продукции: ");
                string? productParam1Input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(productParam1Input))
                {
                    Console.WriteLine("Ошибка: Не введен первый параметр продукции");
                    return;
                }
                double productParam1 = double.Parse(productParam1Input);

                Console.Write("Введите второй параметр продукции: ");
                string? productParam2Input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(productParam2Input))
                {
                    Console.WriteLine("Ошибка: Не введен второй параметр продукции");
                    return;
                }
                double productParam2 = double.Parse(productParam2Input);

                int result = CalculateMaterialRequired(
                    productTypeId,
                    materialTypeId,
                    productQuantity,
                    productParam1,
                    productParam2,
                    connectionString);

                if (result == -1)
                {
                    Console.WriteLine("\nОшибка: Невозможно выполнить расчет. Проверьте введенные данные.");
                }
                else
                {
                    Console.WriteLine($"\nРезультат: Необходимо {result} единиц материала");
                }
            }
            catch (FormatException)
            {
                Console.WriteLine("Ошибка: Неверный формат введенных данных. Введите число.");
            }
            catch (OverflowException)
            {
                Console.WriteLine("Ошибка: Введено слишком большое или слишком маленькое число.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }

            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        public static int CalculateMaterialRequired(
        int productTypeId,
        int materialTypeId,
        int productQuantity,
        double productParam1,
        double productParam2,
        string connectionString)
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    // Получаем коэффициент типа продукции
                    decimal typeCoefficient = 0;
                    string productTypeQuery = "SELECT type_coefficient FROM product_types WHERE product_type_id = @id";
                    using (var cmd = new NpgsqlCommand(productTypeQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@id", productTypeId);
                        var result = cmd.ExecuteScalar();
                        if (result == null || result == DBNull.Value)
                        {
                            Console.WriteLine("Тип продукции с указанным ID не найден");
                            return -1;
                        }
                        typeCoefficient = Convert.ToDecimal(result);
                    }

                    // Получаем процент брака материала
                    decimal defectPercentage = 0;
                    string materialTypeQuery = "SELECT defect_percentage FROM material_types WHERE material_type_id = @id";
                    using (var cmd = new NpgsqlCommand(materialTypeQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@id", materialTypeId);
                        var result = cmd.ExecuteScalar();
                        if (result == null || result == DBNull.Value)
                        {
                            Console.WriteLine("Тип материала с указанным ID не найден");
                            return -1;
                        }
                        defectPercentage = Convert.ToDecimal(result);
                    }

                    // Проверка входных параметров
                    if (productQuantity <= 0 || productParam1 <= 0 || productParam2 <= 0)
                    {
                        Console.WriteLine("Один или несколько параметров имеют недопустимое значение (<= 0)");
                        return -1;
                    }

                    // Расчет количества материала на одну единицу продукции
                    double materialPerUnit = productParam1 * productParam2 * (double)typeCoefficient;

                    // Расчет общего количества материала с учетом брака
                    double totalMaterial = materialPerUnit * productQuantity;
                    double materialWithDefect = totalMaterial / (1 - (double)defectPercentage);

                    // Округляем вверх до целого числа
                    return (int)Math.Ceiling(materialWithDefect);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при расчете: {ex.Message}");
                return -1;
            }
        }
    }
}