using Npgsql;

namespace MaterialCalculatorConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = "Host=localhost;Username=postgres;Password=****;Database=master_floor";

            // Данная строка выводит текстовую информацию пользователю
            Console.WriteLine("Калькулятор необходимого количества материала");

            // Используем try catch для вывода ошибки (а они 100% будут)
            try
            {
                //Выводим пользователю информацию о том, значение какого атрибута надо ввести

                // ID типа продукции (из БД)
                Console.Write("Введите ID типа продукции: ");
                //  Console.ReadLine() считывает вводимые данные и присваивает их переменной productTypeIdInput
                string? productTypeIdInput = Console.ReadLine();
                // Если переменная productTypeIdInput (данные которые мы ввели) равна NULL, то выводим сообщение об ошибке
                if (string.IsNullOrWhiteSpace(productTypeIdInput))
                {
                    Console.WriteLine("Ошибка: Не введен ID типа продукции");
                    return;
                }
                // Если переменная NOT NULL (не пустая), то переводим в тип INT и присваиваем это значение переменной productTypeId для дальнейшего использования в запросе в БД
                int productTypeId = int.Parse(productTypeIdInput);

                // ID Типа материала (из БД)
                Console.Write("Введите ID типа материала: ");
                string? materialTypeIdInput = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(materialTypeIdInput))
                {
                    Console.WriteLine("Ошибка: Не введен ID типа материала");
                    return;
                }
                int materialTypeId = int.Parse(materialTypeIdInput);

                // Количество продукции (любое число, не из БД)
                Console.Write("Введите количество продукции: ");
                string? productQuantityInput = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(productQuantityInput))
                {
                    Console.WriteLine("Ошибка: Не введено количество продукции");
                    return;
                }
                int productQuantity = int.Parse(productQuantityInput);

                // Первый параметр продукции (любое число, не из БД)
                Console.Write("Введите первый параметр продукции: ");
                string? productParam1Input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(productParam1Input))
                {
                    Console.WriteLine("Ошибка: Не введен первый параметр продукции");
                    return;
                }
                // Там, где мы вводим свои данные (не из БД, рандомные) по ТЗ требуется принимать на ввод вещественные, положительные числа, а возвращать целое число (result)
                double productParam1 = double.Parse(productParam1Input);

                // Второй параметр продукции (любое число, не из БД)
                Console.Write("Введите второй параметр продукции: ");
                string? productParam2Input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(productParam2Input))
                {
                    Console.WriteLine("Ошибка: Не введен второй параметр продукции");
                    return;
                }
                double productParam2 = double.Parse(productParam2Input);

                // Присваиваем переменной result (тип int, требование по ТЗ) функицю CalculateMaterialRequired (написали метод ниже, там происходят все вычисления)
                int result = CalculateMaterialRequired(
                    productTypeId,
                    materialTypeId,
                    productQuantity,
                    productParam1,
                    productParam2,
                    connectionString);

                // Обработка ошибок. Можно опустить, если уверены, что комиссия будет вводить 100% верные значения
                // Проверка на отрицательный результат обязателен по ТЗ
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

        // Присваиваем переменные к методу, чтобы можно было их использовать
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
                    double typeCoefficient = 0;
                    // Выбираем коэффицент из таблицы product_types, где product_type_id равен тому id, который мы ввели
                    string productTypeQuery = "SELECT type_coefficient FROM product_types WHERE product_type_id = @id";
                    using (var cmd = new NpgsqlCommand(productTypeQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@id", productTypeId);
                        var result = cmd.ExecuteScalar();
                        // Если такого ID типа продукции нет, то выводим предупреждение об этом
                        if (result == null || result == DBNull.Value)
                        {
                            Console.WriteLine("Тип продукции с указанным ID не найден");
                            return -1;
                        }
                        // Конвертируем результат в double
                        typeCoefficient = Convert.ToDouble(result);
                    }

                    // Получаем процент брака материала
                    double defectPercentage = 0;
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
                        defectPercentage = Convert.ToDouble(result);
                    }

                    // Проверка входных параметров (рандомные данные, проверка на положительное число)
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

                    // Округляем вверх до целого числа и присваиваем тип INT, как требуется в ТЗ
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