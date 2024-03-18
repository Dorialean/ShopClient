using Bogus;
using Newtonsoft.Json;
using ShopClient.Models;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;

const string REGISTER_ENDPOINT = "register";
const string LOGIN_ENDPOINT = "login";
const string PRODUCTS_ENDPOINT = "products";
const string CART_ENDPOINT = "my/cart";
const string ORDER_ENDPOINT = "my/orders";

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
        var user = GenerateUser();

        await RegisterUserAsync(client, user);
        await LoginUserAsync(client, user);
        List<Product> products = (await GetAllProductsAsync(client)).ToList();
        for (int i = 0; i < 4; i++)
            await GetProductAsync(client, products[rnd.Next(0, products.Count)].Name);

        var randomProductIds = products.Where(_ => rnd.NextDouble() > 0.5).Select(p => p.Id).ToList();
        foreach (var id in randomProductIds)
            await AddProductToCartAsync(client, id);
        await RemoveProductFromCartAsync(client, randomProductIds[rnd.Next(0, randomProductIds.Count)]);
        await GetCartAsync(client);

        await AddOrderAsync(client);
        await GetOrderAsync(client);
        if (rnd.NextDouble() > 0.5)
        {
            await CancelOrderAsync(client, 1);
        }
    }
    catch { continue; }
}

static async Task RegisterUserAsync(HttpClient client, User user)
    => await client.PostAsJsonAsync(REGISTER_ENDPOINT, user);

static async Task LoginUserAsync(HttpClient client, User user)
    => await client.PostAsJsonAsync(LOGIN_ENDPOINT, new
    {
        email = user.Email,
        password = user.Password,
    });

static async Task<IEnumerable<Product>> GetAllProductsAsync(HttpClient client)
{
    var productsResponse = await client.GetAsync("products");
    var productsRaw = await productsResponse.Content.ReadAsStringAsync();
    var products = JsonConvert.DeserializeObject<IEnumerable<Product>>(productsRaw);
    return products;
}

static async Task GetProductAsync(HttpClient client, string productName)
    => await client.GetAsync($"{PRODUCTS_ENDPOINT}/?title={productName}");

static async Task AddProductToCartAsync(HttpClient client, int productId)
    => await client.PutAsJsonAsync(CART_ENDPOINT + "add", new
    {
        item_id = productId,
        quantity = 1,
    });

static async Task RemoveProductFromCartAsync(HttpClient client, int productId)
{
    var request = new HttpRequestMessage(HttpMethod.Delete, CART_ENDPOINT + "/remove")
    {
        Content = new StringContent(JsonConvert.SerializeObject(new { item_id = productId }), Encoding.UTF8, "application/json")
    };
    await client.SendAsync(request);
}

static async Task GetCartAsync(HttpClient client) => await client.GetAsync(CART_ENDPOINT);

static async Task GetOrderAsync(HttpClient client) => await client.GetAsync(ORDER_ENDPOINT);

async Task AddOrderAsync(HttpClient client) => await client.PostAsJsonAsync(ORDER_ENDPOINT + "/add", new
{
    delivery_address = faker.Address,
});

static async Task CancelOrderAsync(HttpClient client, int orderId)
{
    var request = new HttpRequestMessage(HttpMethod.Delete, ORDER_ENDPOINT + "/remove")
    {
        Content = new StringContent(JsonConvert.SerializeObject(new { order_id = orderId }), Encoding.UTF8, "application/json")
    };
    await client.SendAsync(request);
}




User GenerateUser() => new()
{
    Name = faker.Person.FullName,
    Email = faker.Person.Email,
    Password = GeneratePassword(120),
};


static string GeneratePassword(int length)
{
    byte[] rgb = new byte[length];
    using (RNGCryptoServiceProvider rngCrypt = new())
    {
        rngCrypt.GetBytes(rgb);
    }
    return Convert.ToBase64String(rgb);
}