using Bogus;
using ShopClient.Models;

using HttpClient client = new()
{
    BaseAddress = new Uri(args[0]),
};
var faker = new Faker();
var rnd = new Random();

while (true)
{
    try
    {
        var user = new User();

        await user.RegisterUserAsync(client);
        await user.LoginUserAsync(client);

        var products = (await user.GetAllProductsAsync(client)).ToList();
        // NOTE: Add random number of products to cart (from 4 to 100 -> can add same products multiple times)
        for (int i = 0; i < rnd.Next(4, 50); i++)
        {
            var product = await user.GetProductAsync(client, products[rnd.Next(0, products.Count)].Name);
            await user.AddProductToCartAsync(client, product.Id);
        }

        var cart = await user.GetCartAsync(client);
        
        // NOTE: Remove random number of products from cart
        // TASK: Предположение: модель пользователя-шопоголика подразумевает “муки выбора” и частое изменение корзины.
        var randomNumberOfIdsToDelete = GetRandomNumberOfIds(cart);
        foreach (var id in randomNumberOfIdsToDelete)
        {
            await user.RemoveProductFromCartAsync(client, id);
        }

        // NOTE: Place order
        var orderId = await user.AddOrderAsync(client, user.DeliveryAddress);
        await user.GetOrderAsync(client);
        
        // NOTE: Cancel order with 50% chance
        if (rnd.NextDouble() > 0.5)
        {
            await user.CancelOrderAsync(client, orderId);
        }

        // NOTE: Logout user to clear session from backend
        await user.LogoutUserAsync(client);
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Operation canceled");
        break;
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
    }
}

static List<int> GetRandomNumberOfIds(List<CartItem> cartItems)
{
    var rnd = new Random();
    int listLength = rnd.Next(0, cartItems.Count - 1); // Generate random length for the list
        
    List<int> idsList = new List<int>();
    foreach (var item in cartItems)
    {
        for (int i = 0; i < item.Quantity; i++)
        {
            idsList.Add(item.Id); // Add item ID based on its quantity
        }
    }

    // Shuffle the list to randomize which IDs are included
    idsList = idsList.OrderBy(x => rnd.Next()).ToList();
        
    // Trim the list to the randomly determined length
    if (idsList.Count > listLength)
    {
        idsList = idsList.Take(listLength).ToList();
    }
    
    return idsList;
}