GitHub Copilot Chat Előzmények - RentGrid Projekt (11. rész)

Ez a dokumentum a GitHub Copilot-tal folytatott beszélgetést tartalmazza

1. Beszélgetés: Felhasználói fiókok inaktiválása

Prompt

Készíts egy admin funkciót, amivel az adminisztrátor inaktiválni tudja a felhasználók fiókját.

 GitHub Copilot

 Gondolatmenet (Thinking)

A funkció megvalósításához módosítani kell a felhasználói modellt, a backend vezérlőt és a frontend adminisztrációs felületet.

Adatmodell: Hozzá kell adni egy IsActive (bool) mezőt a User entitáshoz.

Backend: Létre kell hozni egy végpontot a státusz átbillentéséhez, és módosítani kell a bejelentkezési logikát, hogy az inaktív felhasználókat elutasítsa.

Frontend: Az admin felhasználó-listájában el kell helyezni egy kapcsolót vagy gombot az inaktiváláshoz.

 Eszközhasználat

cat RentGrid.Api/Models/User.cs: A felhasználói modell ellenőrzése.

cat RentGrid.Api/Controllers/AdminController.cs: Az adminisztrátori műveletek bővítése.

 Válasz

Frissítettem a rendszert a fiókkezelési funkcióval. Mostantól az adminisztrátorok egy kattintással letilthatják a hozzáférést.

AdminController.cs (Részlet):

[HttpPatch("users/{id}/status")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> ToggleUserStatus(string id, [FromBody] bool isActive)
{
    var user = await _userManager.FindByIdAsync(id);
    if (user == null) return NotFound();

    user.IsActive = isActive;
    await _userManager.UpdateAsync(user);

    return Ok(new { Message = $"Felhasználó {(isActive ? "aktiválva" : "inaktiválva")}." });
}


2. Beszélgetés: Bejelentkezési korlátozás bevezetése

Prompt

Gondoskodjunk róla, hogy az inaktív felhasználók ne tudjanak belépni, még akkor sem, ha jó a jelszavuk.

 GitHub Copilot

 Válasz

Módosítottam az AuthController bejelentkezési metódusát. A jelszó ellenőrzése után most már a fiók státuszát is vizsgálja a rendszer.

if (!user.IsActive)
{
    return Unauthorized(new { Error = "A fiókja inaktiválva lett. Kérjük, vegye fel a kapcsolatot az ügyfélszolgálattal." });
}


3. Beszélgetés: Frontend Admin felület frissítése

Prompt

Adjunk hozzá egy vizuális jelzést a felhasználói listához az admin felületen, ami mutatja a státuszt.

 GitHub Copilot

 Válasz

Frissítettem a felhasználókezelő komponenst. A státuszváltáshoz egy stílusos kapcsolót (toggle) és színkódolt címkéket kapott a felület.

user-management.component.html:

<tr *ngFor="let user of users">
  <td>{{ user.email }}</td>
  <td>
    <span [class]="user.isActive ? 'badge bg-success' : 'badge bg-danger'">
      {{ user.isActive ? 'Aktív' : 'Inaktív' }}
    </span>
  </td>
  <td>
    <button (click)="toggleStatus(user)" class="btn btn-sm" 
            [class.btn-outline-danger]="user.isActive"
            [class.btn-outline-success]="!user.isActive">
      {{ user.isActive ? 'Inaktiválás' : 'Aktiválás' }}
    </button>
  </td>
</tr>
