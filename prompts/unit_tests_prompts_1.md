GitHub Copilot Chat Előzmények - RentGrid Projekt (5. rész)

Ez a dokumentum a GitHub Copilot-tal folytatott beszélgetést tartalmazza

1. Beszélgetés: AuthController Egységtesztek (NUnit)

Prompt

Készíts egységteszteket az AuthController-hez NUnit keretrendszerrel.

 GitHub Copilot

 Gondolatmenet (Thinking)

Az AuthController teszteléséhez szükség van a függőségek (Dependencies) izolálására. Az alábbi szempontokat kell figyelembe venni:

Moq keretrendszer: A UserManager, SignInManager és az IConfiguration mockolása szükséges.

NUnit annotációk: [TestFixture], [SetUp], [Test].

Teszt esetek: Sikeres bejelentkezés, sikertelen bejelentkezés (rossz jelszó), regisztráció validációja.

🛠 Eszközhasználat

cat RentGrid.Api/Controllers/AuthController.cs: A vezérlő logikájának elemzése.

cat RentGrid.Api.Tests/RentGrid.Api.Tests.csproj: A teszt projekt függőségeinek ellenőrzése.

 Válasz

Elkészítettem az AuthControllerTests.cs fájlt, amely lefedi az alapvető autentikációs folyamatokat.

AuthControllerTests.cs (Részlet):

[TestFixture]
public class AuthControllerTests
{
    private Mock<UserManager<User>> _mockUserManager;
    private Mock<IAuthService> _mockAuthService;
    private AuthController _controller;

    [SetUp]
    public void Setup()
    {
        _mockUserManager = MockUserManager<User>();
        _mockAuthService = new Mock<IAuthService>();
        _controller = new AuthController(_mockUserManager.Object, _mockAuthService.Object);
    }

    [Test]
    public async Task Login_WithValidCredentials_ReturnsOk()
    {
        // Arrange
        var loginDto = new LoginDto { Email = "test@example.com", Password = "[testpass]" };
        _mockAuthService.Setup(s => s.AuthenticateAsync(loginDto))
            .ReturnsAsync(new AuthResponse { Success = true, Token = "fake-jwt-token" });

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        Assert.IsInstanceOf<OkObjectResult>(result);
    }
}


2. Beszélgetés: Mocking UserManager

Prompt

Hogyan tudom mockolni a UserManager-t, mivel nincs neki interfésze?

 GitHub Copilot

 Gondolatmenet (Thinking)

A UserManager<T> osztályt nehéz közvetlenül mockolni, mert sok függősége van (UserStore, Options, PasswordHasher stb.). A megoldás egy segédmetódus (Helper method) használata, amely létrehozza a mock példányt a szükséges alapértelmezett paraméterekkel.

 Válasz

Íme egy bevált módszer a UserManager mockolására:

public static Mock<UserManager<TUser>> MockUserManager<TUser>() where TUser : class
{
    var store = new Mock<IUserStore<TUser>>();
    return new Mock<UserManager<TUser>>(store.Object, null, null, null, null, null, null, null, null);
}


3. Beszélgetés: Tesztfuttatás és Eredmények

Prompt

Futtasd le a teszteket, hogy lássuk az eredményt.

 GitHub Copilot

 Eszközhasználat

dotnet test: A tesztprojekt futtatása.

 Válasz

A tesztelés befejeződött, minden eset sikeresen lefutott.

Teszteredmények:

Összes teszt: 12

Sikeres: 12

Sikertelen: 0

Időtartam: 1.2 másodperc
