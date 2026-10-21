<div dir="rtl">

# מערכת לניהול פניות – מבדק Full Stack

[![CI](https://github.com/mirikorla/RequestsManagement/actions/workflows/ci.yml/badge.svg)](https://github.com/mirikorla/RequestsManagement/actions/workflows/ci.yml)

מערכת לניהול פניות מארגונים: חיפוש, סינון, מיון ודפדוף בצד השרת על 100,000 רשומות, עדכון סטטוס עם Optimistic Concurrency, היסטוריית שינויים, עדכון מרוכז (Bulk) ונתונים מסכמים עם Cache.

| מסמך | תוכן |
|---|---|
| [docs/WORK_PLAN.md](docs/WORK_PLAN.md) | תכנון עבודה: Epics, משימות, תלויות, הערכת מאמץ, מיפוי לאפיון |
| [docs/PERFORMANCE.md](docs/PERFORMANCE.md) | אינדקסים, שיטת מדידה, צווארי בקבוק |
| [docs/perf/measure.ps1](docs/perf/measure.ps1) | סקריפט המדידה: logical reads, Execution Plan בפועל וזמן ממוצע לכל שאילתה |
| [docs/perf/measure.sql](docs/perf/measure.sql) | אותן שאילתות להרצה ב-SSMS עם Execution Plan גרפי |

**תוכן עניינים:**
[טכנולוגיות וגרסאות](#טכנולוגיות-וגרסאות) ·
[הרצה](#הרצה) ·
[מבנה הפתרון](#מבנה-הפתרון) ·
[API](#api) ·
[בסיס הנתונים](#בסיס-הנתונים) ·
[Concurrency](#concurrency) ·
[Bulk Update](#bulk-update--partial-success) ·
[Cache](#cache) ·
[Angular](#angular) ·
[החלטות טכנולוגיות](#החלטות-טכנולוגיות-וחלופות) ·
[מגבלות ושיפורים](#מגבלה-ידועה-ומה-אפשר-לשפר) ·
[שימוש ב-AI](#שימוש-בכלי-ai)

## טכנולוגיות וגרסאות

| שכבה | טכנולוגיה |
|---|---|
| Backend | .NET 10 (SDK 10.0.402), ASP.NET Core Web API, EF Core 10 |
| Validation | FluentValidation 12 |
| בסיס נתונים | SQL Server 2022 (פותח ונבדק על SQL Server 2022 Express) |
| Frontend | Angular 22 (standalone components, signals, zoneless), RxJS 7.8, TypeScript 6 |
| כלי בנייה ל-Frontend | Node.js 24, npm |
| בדיקות | xUnit, WebApplicationFactory, Testcontainers (SQL Server אמיתי) |
| הרצה | הרצה מקומית ב-Windows; Docker Compose ו-nginx |
| CI | GitHub Actions: בנייה, כל הבדיקות (כולל Integration מול SQL Server ב-Docker), בניית Angular, והרצת Docker Compose מלאה, בכל העלאה |

## הרצה

### אפשרות 1: הרצה מקומית ב-Windows עם SQL Server Express (הדרך שנבדקה)

זו הדרך שבה המערכת פותחה ונבדקה, כולל כל הבדיקות האוטומטיות והמדידות. דרישות: ‏.NET SDK 10 ו-Node.js 24.

**1. הורדת הפרויקט:**

```powershell
git clone https://github.com/mirikorla/RequestsManagement.git
cd RequestsManagement
```

**2. התקנת SQL Server Express** (פעם אחת; אם מופיע חלון אישור, לאשר):

```powershell
winget install --id Microsoft.SQLServer.2022.Express --source winget
```

השרת מותקן כשירות Windows בשם `SQLEXPRESS` ועולה אוטומטית עם המחשב. ההתחברות היא עם חשבון Windows, בלי סיסמה.

**3. השרת (API)**, בחלון PowerShell אחד, בתיקיית הפרויקט (`RequestsManagement`):

```powershell
$env:ConnectionStrings__Default="Server=.\SQLEXPRESS;Database=RequestsManagement;Trusted_Connection=True;TrustServerCertificate=True"
dotnet run --project src\RequestsManagement.Api
```

בהרצה הראשונה נוצרים מסד הנתונים והטבלאות, ו-100,000 פניות (כ-10 שניות). ממתינים לשורה `Now listening on: http://localhost:5045`.

**4. הממשק (Angular)**, בחלון PowerShell שני, בתיקיית הפרויקט:

```powershell
cd web
npm.cmd install
npm.cmd start
```

ממתינים לשורה `Local: http://localhost:4200`, ואז פותחים את http://localhost:4200.

> ב-PowerShell משתמשים ב-`npm.cmd` ולא ב-`npm`: ‏Windows חוסם כברירת מחדל את `npm.ps1` ("running scripts is disabled on this system"), ו-`npm.cmd` עוקף את זה בלי לשנות הגדרות אבטחה.

- ממשק: http://localhost:4200
- Swagger (כל ה-API, אפשר לנסות ממנו קריאות): http://localhost:5045/swagger

עצירה: ‏Ctrl+C בכל אחד מהחלונות.

### אפשרות 2: Docker

> **איך זה נבדק:** במחשב הפיתוח Docker לא היה זמין (הווירטואליזציה כבויה ב-BIOS), ולכן האפשרות הזו נבדקת **אוטומטית ב-CI**, בשרתי Linux של GitHub: בכל העלאה מורצת `docker compose up`, ונבדק שהממשק מוגש ושבקשה דרכו מחזירה את 100,000 הפניות. ב-Windows עם Docker Desktop היא לא נבדקה ידנית.

דרישה: Docker Desktop.

```bash
docker compose up --build
```

- ממשק: http://localhost:8080
- Swagger: http://localhost:5045/swagger

אפשר גם להריץ רק את בסיס הנתונים ב-Docker (`docker compose up -d sql`), ואת השרת והממשק כמו באפשרות 1, עם מחרוזת החיבור שב-`appsettings.json`.

> הסיסמה שב-`docker-compose.yml` וב-`appsettings.json` (`Local_Dev_Passw0rd!`) שייכת רק ל-SQL Server הזמני שבקונטיינר המקומי, ואינה סיסמה של שום סביבה אמיתית. באפשרות 1 אין סיסמה כלל: ההתחברות היא עם חשבון Windows.

### יצירה חוזרת של נתוני הבדיקה

כשהשרת לא רץ, בחלון PowerShell שבו הוגדרה מחרוזת החיבור (שלב 3 באפשרות 1):

```powershell
dotnet run --project src\RequestsManagement.Api -- --reseed          # 100,000 רשומות
dotnet run --project src\RequestsManagement.Api -- --reseed 500000   # כמות אחרת
```

הפקודה מוחקת את הפניות ואת ההיסטוריה ויוצרת אותן מחדש. הנתונים דטרמיניסטיים (Random עם Seed קבוע), כך שמדידות חוזרות ניתנות להשוואה.

### בדיקות

56 בדיקות אוטומטיות, וכולן רצות ב-CI בכל העלאה:

| איפה | כמה | מה הן בודקות |
|---|---:|---|
| שרת, בדיקות יחידה | 23 | חוקי המעבר, Audit ב-ServiceRequest, Validators. רצות בלי בסיס נתונים |
| שרת, בדיקות Integration | 14 | שרת אמיתי מול SQL Server אמיתי: סינון, 400/404/422, עדכונים מתחרים, Bulk, היסטוריה, Cache |
| Angular (Vitest) | 19 | ‏Debounce, ביטול בקשה ישנה (switchMap), טיפול ב-409, מצבי ריק ושגיאה, הפרמטרים שה-Service שולח, ופונקציות העזר |

**איך הם משלימים זה את זה:**
- **בדיקות השרת** בודקות שהשרת **מחליט ושומר** נכון: החוקים העסקיים, השמירה בבסיס הנתונים וה-Concurrency.
- **בדיקות ה-Angular** בודקות שהממשק **שולח ומגיב** נכון. השרת מוחלף ב-`HttpTestingController`, שבו הבדיקה קובעת מה ה-API "עונה". כך אפשר לבדוק גם מצבים שקשה לייצר במציאות, כמו 409, שגיאת שרת או תשובה שמתעכבת.
- **החיבור בין השניים** נבדק ב-CI: המערכת כולה עולה ב-Docker Compose, ובקשה שעוברת דרך הממשק מגיעה לשרת ומחזירה את 100,000 הפניות.

**בדיקות השרת, עם SQL Server Express**, ב-PowerShell:

```powershell
$env:INTEGRATION_SQL="Server=.\SQLEXPRESS;Database=RequestsTests;Trusted_Connection=True;TrustServerCertificate=True"
dotnet test
```

בדיקות ה-Integration מוחקות ויוצרות מחדש את הנתונים במסד שב-`INTEGRATION_SQL`, לכן יש להשתמש **במסד ייעודי לבדיקות** (`RequestsTests`), לא במסד שהמערכת עובדת מולו. מסד שלא קיים נוצר אוטומטית.

**בלי `INTEGRATION_SQL`**, בדיקות ה-Integration מרימות SQL Server זמני ב-Docker (Testcontainers). כך הן רצות ב-CI בכל העלאה. בדיקות היחידה לבדן רצות בלי שום תלות:

```bash
dotnet test tests/RequestsManagement.UnitTests
```

**בדיקות ה-Angular**, בתיקייה `web` (לא דורשות שרת: התשובות של ה-API מדומות):

```powershell
npm.cmd test -- --watch=false
```

> אם השרת (API) רץ באותו זמן, הבנייה של הבדיקות עלולה להיכשל בשגיאה "The file is locked by: RequestsManagement.Api". הפתרון: לעצור את השרת לפני הרצת הבדיקות, או לבנות לתיקייה נפרדת: `dotnet test --artifacts-path .\.test-artifacts`.

## מבנה הפתרון

```
src/
  RequestsManagement.Core              ליבה. ללא תלות ב-EF או ב-ASP.NET
    Models/          אובייקטי בסיס הנתונים: ServiceRequest (כולל ChangeStatus + Audit), RequestStatusHistory,
                     וחוקי המעבר בין סטטוסים (StatusTransitions)
    DTOs/            מה שה-API מקבל ומחזיר ללקוח: RequestDto, RequestQueryDto, PagedResultDto, UpdateStatusDto,
                     StatusHistoryDto, RequestsSummaryDto
      Bulk/          BulkUpdateStatusDto, BulkItemDto, BulkItemResultDto, BulkUpdateStatusResultDto
    Enums/           RequestStatus, Priority, RequestSortField, SortDirection, BulkItemOutcome, StatusChangeOutcome
    Interfaces/      IRequestQueryService, IRequestStatusService, ISummaryService
    Validation/      Validator לכל קלט + RowVersionFormat (כלל משותף)
    Exceptions/      NotFoundException, InvalidStatusTransitionException, ConcurrencyConflictException

  RequestsManagement.Infrastructure    מימוש מול SQL Server
    DAL/             שכבת הגישה לנתונים: AppDbContext (סכמה + אינדקסים), Migrations, DataSeeder
    Services/        רק Services, ולכל אחד ממשק ב-Core/Interfaces:
                     RequestQueryService (קריאה), RequestStatusService (כתיבה + Concurrency),
                     SummaryService (Aggregations + Cache)
    Queries/         RequestQueryBuilder: בונה את השאילתה (סינון, מיון, דפדוף) כ-IQueryable
    Mapping/         RequestMapping: המרה מ-Models ל-DTOs, במקום אחד
    DependencyInjection.cs   AddInfrastructure()

  RequestsManagement.Api               שכבת HTTP בלבד
    Controllers/     RequestsController (דק: מעביר לשירותים)
    ErrorHandling/   GlobalExceptionHandler (חריגות → ProblemDetails)
    Filters/         ValidationFilter: המנגנון שמפעיל אוטומטית, לפני כל פעולה, את ה-Validator המתאים מ-Core/Validation
    Startup/         AddApi(), DatabaseInitializer (Migrations + Seed)
    Program.cs       סדר ההפעלה בלבד

tests/
  RequestsManagement.UnitTests         חוקי המעבר, Audit ב-ServiceRequest, Validators
  RequestsManagement.IntegrationTests  API + SQL Server אמיתי: סינון, 400/404/422, Concurrency, Bulk, היסטוריה, Cache

web/src/app/                           Angular. לכל קומפוננטה תיקייה עם .component.ts / .html / .scss
                                       (ובדיקות ב-.spec.ts לצד הקובץ שהן בודקות)
  components/
    requests-page/       הקומפוננטה הראשית: מחזיקה את ה-State ואת זרם ה-RxJS
    requests-table/      הטבלה                    ┐
    request-filters/     טופס המסננים (Debounce)   │
    pager/               הדפדוף                   ├ תצוגה בלבד (input/output), לא מכירות את השרת
    summary-panel/       הנתונים המסכמים           │
    bulk-action-bar/     פס העדכון המרוכז          ┘
    history-dialog/      חלון ההיסטוריה: טוען את הנתונים שלו בעצמו, נפתח לפי דרישה
  services/    requests-api.service.ts    המקום היחיד שמדבר עם השרת
  models/      קובץ לכל DTO, באותם שמות כמו בשרת (Core/DTOs): request.dto.ts, request-query.dto.ts,
               paged-result.dto.ts, update-status.dto.ts, status-history.dto.ts, requests-summary.dto.ts,
               ו-bulk/ (bulk-item, bulk-update-status, bulk-item-result, bulk-update-status-result)
  enums/       קובץ לכל Enum, כמו בשרת (Core/Enums): request-status, priority, request-sort-field,
               sort-direction, bulk-item-outcome
  constants/   status-transitions.ts      חוקי המעבר (העתק של StatusTransitions, רק להצגת אפשרויות)
               labels.ts                  הטקסטים בעברית לסטטוסים ולעדיפויות
  helpers/     list-state.ts              מצב הרשימה (Loading / Data / Error) והשאילתה ההתחלתית
               status-messages.ts         כל ההודעות למשתמשת אחרי עדכון (כולל 409 ו-Bulk)
               describe-error.ts          הופך שגיאת HTTP להודעה קריאה
```

**עקרונות החלוקה:**
- **קובץ לכל טיפוס**, ושם הקובץ זהה לשם הטיפוס.
- **שמות לפי תפקיד:** כל אובייקט בתיקיית `DTOs` מסתיים ב-`Dto`, וכל Validator נקרא על שם ה-DTO שהוא בודק (`UpdateStatusDto` ← `UpdateStatusDtoValidator`). ‏Enums נמצאים בתיקייה משלהם (`Enums`) ובלי סיומת, כי הם רשימת ערכים ולא אובייקט שמעביר נתונים.
- **אחריות אחת לכל קלאס.** למשל, `RequestQueryBuilder` רק *בונה* את השאילתה ו-`RequestQueryService` רק *מריץ* אותה. ‏`RequestsController` לא מכיל לוגיקה, ו-`Program.cs` מכיל רק את סדר ההפעלה.
- **כיוון תלות אחד:** ‏Api → Infrastructure → Core. ‏Core לא מכיר אף שכבה אחרת.
- **ממשקים רק בגבול בין שכבות**, לא לכל קלאס. לכל Service יש ממשק, כי ה-Controller תלוי בו. כלי עזר סטטיים (`RequestQueryBuilder`, ‏`RequestMapping`) בלי ממשק: אין להם מימוש חלופי, ובקלאס סטטי אי אפשר לממש ממשק.

שלוש שכבות ולא Clean Architecture מלאה: אין כאן Repository מעל EF או CQRS/MediatR. ‏EF Core כבר מממש Unit of Work ו-Repository, ושכבה נוספת הייתה מוסיפה קוד בלי ערך (האפיון מזהיר מ-Over-Engineering).

### API

| Method | Route | תיאור |
|---|---|---|
| GET | `/api/requests` | עמוד פניות. פרמטרים: `page`, `pageSize` (עד 100), `search`, `status`, `priority`, `organizationName` (מתחיל ב…), `assignedTo`, `createdFrom`, `createdTo`, `sortBy` (CreatedAt/UpdatedAt/Priority/Status/Title/OrganizationName), `sortDirection` (Asc/Desc) |
| GET | `/api/requests/summary` | ספירה לפי סטטוס ולפי עדיפות (Cache) |
| GET | `/api/requests/{id}` | פנייה בודדת |
| GET | `/api/requests/{id}/history` | היסטוריית שינויי סטטוס |
| PATCH | `/api/requests/{id}/status` | `{ status, rowVersion, changedBy }` |
| POST | `/api/requests/status/bulk` | `{ status, changedBy, items: [{ id, rowVersion }] }`, עד 100 פריטים |

קודי תשובה: `400` קלט לא תקין (ValidationProblemDetails עם פירוט לפי שדה), `404` פנייה לא קיימת, `409` עדכון מתחרה (כולל המצב העדכני בשדה `current`), `422` מעבר סטטוס לא מותר (כולל `allowedTransitions`), `500` שגיאה כללית בלי חשיפת פרטים. כל השגיאות בפורמט ProblemDetails (RFC 7807), וממופות במקום אחד: `GlobalExceptionHandler`.

**מעברי סטטוס מותרים:** New → InProgress/Waiting · InProgress → Waiting/Completed · Waiting → InProgress/Completed · Completed → InProgress (פתיחה מחדש).

## בסיס הנתונים

**SQL Server** נבחר כי הנתונים טבלאיים ובעלי מבנה קבוע, ונדרשים סינון משולב, מיון ו-Aggregations. בנוסף, יש לו `rowversion` מובנה שמתאים בדיוק ל-Optimistic Concurrency, ותמיכה מלאה ב-EF Core. נשקל MongoDB: הוא לא מוסיף כאן יתרון (אין מסמכים בעלי מבנה משתנה), והטרנזקציה של "עדכון + Audit" פשוטה יותר ב-SQL.

- `Requests`: ‏Status ו-Priority נשמרים כ-`tinyint`, התאריכים כ-`datetime2(3)` ב-UTC, ו-`RowVersion` מסוג `rowversion`.
- `RequestStatusHistory`: ‏RequestId, PreviousStatus, NewStatus, ChangedAt, ChangedBy.

**האינדקסים, לפי התרחיש שכל אחד משרת:**

| אינדקס | תרחיש |
|---|---|
| `IX_Requests_CreatedAt` | מסך ברירת המחדל (החדשות קודם) וסינון לפי טווח תאריכים |
| `IX_Requests_Status_CreatedAt` | סינון לפי סטטוס, כבר ממוין לפי תאריך. גם הספירה לפי סטטוס |
| `IX_Requests_Priority_CreatedAt` | סינון לפי עדיפות, כבר ממוין לפי תאריך. גם הספירה לפי עדיפות |
| `IX_Requests_AssignedTo_CreatedAt` | "הפניות שלי": מטפל וטווח תאריכים בחיפוש אחד |
| `IX_Requests_OrganizationName` | סינון לפי ארגון ("מתחיל ב…") |
| `IX_RequestStatusHistory_RequestId_ChangedAt` | היסטוריה של פנייה, כבר ממוינת לפי זמן |

**במכוון בלי אינדקס:** חיפוש חופשי (`LIKE '%x%'` לא יכול להשתמש באינדקס רגיל) ומיון לפי שדות משניים (כותרת, תאריך עדכון). אינדקס לכל אחד מהם היה מאט כל כתיבה. המדידות, ה-Execution Plans והנימוק המלא נמצאים ב-[docs/PERFORMANCE.md](docs/PERFORMANCE.md).

## Concurrency

- כל `RequestDto` מכיל את `RowVersion` (base64), והלקוח מחזיר אותו בעדכון.
- השרת מציב את הערך כ-**Original Value** של ה-Entity, ולכן EF מייצר `UPDATE … WHERE Id = @id AND RowVersion = @clientVersion`. אם משתמש אחר עדכן בינתיים, 0 שורות מתעדכנות, EF זורק `DbUpdateConcurrencyException`, והתשובה היא `409` עם המצב העדכני.
- האכיפה נעשית ב**בסיס הנתונים**, לא ב-UI ולא בבדיקה בזיכרון. גם שתי בקשות שקראו את אותה גרסה באותו רגע לא יכולות לדרוס זו את זו, ולכן אין Lost Update.
- `rowversion` מתעדכן אוטומטית ב-SQL Server בכל UPDATE. ‏`UpdatedAt` מתעדכן במודל (`ServiceRequest.ChangeStatus`) יחד עם הסטטוס ורשומת ה-Audit, וכולם נשמרים ב-`SaveChanges` אחד, כלומר טרנזקציה אחת. אין מצב שבו הסטטוס השתנה בלי Audit, או להפך.
- הבדיקה `Concurrent_updates_with_the_same_version_let_exactly_one_win` שולחת שתי בקשות במקביל ומוודאת: אחת `200`, אחת `409`, ורשומת היסטוריה אחת בלבד.
- ב-Angular, תשובת 409 מציגה הודעה עם הסטטוס הנוכחי ומרעננת את הרשימה, כדי שהמשתמש יוכל לנסות שוב על הגרסה העדכנית.

## Bulk Update – Partial Success

**ההחלטה:** הצלחה חלקית. כל פריט נשמר בטרנזקציה משלו, והתשובה היא `200` עם תוצאה לכל פריט:

```json
{ "succeeded": 1, "failed": 2, "results": [
  { "id": 12, "outcome": "Updated", "rowVersion": "AAAAAAAAB9I=" },
  { "id": 13, "outcome": "Conflict", "error": "Modified by another user. Reload and retry." },
  { "id": 99999, "outcome": "NotFound", "error": "Request not found." } ] }
```

ערכים אפשריים ל-`outcome`: ‏`Updated` / `NotFound` / `Conflict` / `InvalidTransition`.

**הנימוק:** הפניות בבקשה בלתי תלויות זו בזו. ב-All-or-Nothing, פנייה אחת שמשתמש אחר עדכן מכשילה את כל ה-100, ובמערכת עם משתמשים רבים במקביל זה יקרה הרבה, והמשתמש ייאלץ לנסות שוב ושוב. בהצלחה חלקית המשתמש מקבל רשימה מדויקת של מה נכשל ולמה, ויכול לטעון מחדש ולנסות שוב רק את אלה. **שגיאת קלט** (יותר מ-100 פריטים, מזהה כפול, סטטוס לא חוקי) נדחית כולה ב-`400` לפני כל עדכון.

**מימוש:** שליפה אחת (`WHERE Id IN (…)`) של כל הפריטים, ואז עדכון של כל פריט עם בדיקת RowVersion משלו. פריט שנכשל ב-Concurrency מנותק מה-DbContext, כדי שלא יישלח שוב ב-SaveChanges הבא. ה-Cache מנוקה פעם אחת אם לפחות פריט אחד הצליח.

**למה `200` גם כשחלק מהפריטים נכשלו:** הבקשה עצמה תקינה ועובדה במלואה, ותוצאת כל פריט נמצאת בגוף התשובה (`succeeded`, `failed`, `results`). חלופה שנשקלה היא `207 Multi-Status`. היא נדחתה כי היא מגיעה מ-WebDAV, ולקוחות HTTP וספריות רבות לא מטפלים בה כראוי. קוד שגיאה (4xx) מוחזר רק כשהבקשה כולה נדחתה בלי שבוצע אף עדכון.

**ביטול באמצע** (למשל הלקוח סגר את החיבור): הבקשה נעצרת דרך ה-`CancellationToken`. פריטים שכבר נשמרו נשארים מעודכנים, כל אחד עם רשומת ה-Audit שלו, ופריטים שעוד לא טופלו לא משתנים. אין פריט שנשאר חצי מעודכן, כי כל פריט נשמר בטרנזקציה משלו. זו התנהגות עקבית עם Partial Success: אם הלקוח לא קיבל תשובה, הוא יכול לטעון את הפניות מחדש ולראות מה עודכן.

## Cache

- **מה נשמר:** תוצאת `/api/requests/summary` (ספירות לפי סטטוס ולפי עדיפות).
- **למה דווקא זה:** כל טעינת מסך של כל משתמש מבקשת אותו. זה `GROUP BY` על כל הטבלה, והוא משתנה רק כשמשתנה סטטוס, אירוע שהשרת יודע עליו. רשימות הפניות **לא** נשמרות ב-Cache: מספר צירופי המסננים כמעט אינסופי, והן כבר מהירות בזכות האינדקסים.
- **Expiration:** ‏5 דקות (Absolute), כרשת ביטחון לשינויים שלא עוברים דרך ה-API (Reseed, עדכון ישיר ב-DB).
- **Invalidation:** כל עדכון סטטוס מוצלח, בודד או Bulk, קורא ל-`ISummaryService.Invalidate()`, והקריאה הבאה מחושבת מחדש. נבדק ב-`Summary_cache_is_invalidated_after_a_status_change`.
- **מניעת שמירה של תוצאה ישנה (Race):** אם סטטוס משתנה **בזמן** שהספירה מחושבת, ה-Invalidation קורה לפני שהתוצאה נשמרת, והתוצאה הישנה הייתה נשארת ב-Cache עד 5 דקות. לכן כל Invalidation מקדם מונה גרסה, ותוצאה נשמרת רק אם המונה לא השתנה מאז תחילת החישוב. אם השתנה, היא מוחזרת למבקש אבל לא נשמרת. נבדק ב-`Counts_computed_across_a_status_change_are_not_cached`, שמכריח את התזמון הזה באופן דטרמיניסטי.
- **בכמה מופעי שרת:** ‏`IMemoryCache` הוא מקומי לכל מופע, ולכן Invalidation במופע A לא מנקה את מופע B, ו-B עלול להחזיר נתון מיושן עד 5 דקות. ההתאמה הנדרשת: מעבר ל-`IDistributedCache` / ‏HybridCache מעל Redis (מפתח משותף, Invalidation אחד לכולם), או השארת Cache מקומי והפצת אירוע Invalidation ב-Redis Pub/Sub. ‏`ISummaryService` הוא נקודת ההחלפה היחידה, ושאר הקוד לא ישתנה.

## Angular

- **צד שרת בלבד:** כל שינוי בחיפוש, סינון, מיון או עמוד יוצר `RequestQueryDto` חדש שנשלח ל-API. הלקוח לא מסנן ולא ממיין נתונים.
- **Debounce:** טופס המסננים פולט ערך רק אחרי `debounceTime(350)` ו-`distinctUntilChanged`.
- **ביטול בקשות קודמות:** ‏`switchMap` בזרם החיפוש. שינוי שאילתה בזמן שבקשה קודמת עדיין רצה מבטל אותה (`withFetch` → ‏AbortController). בשרת, ה-`CancellationToken` מבטל את השאילתה ל-SQL, ו-`GlobalExceptionHandler` מזהה ביטול מצד הלקוח ולא רושם אותו כשגיאה.
- **מצבי UI:** Loading (בטעינה הראשונה הודעה; בטעינות הבאות הטבלה הקודמת נשארת מעומעמת כדי שלא תהבהב), Empty, ו-Error עם כפתור "נסו שוב".
- **עדכון סטטוס:** התפריט מציע רק מעברים מותרים. השרת הוא הסמכות, ומחזיר 422 על מעבר לא מותר. טיפול ב-409 מתואר למעלה.
- **הפרדת אחריות:** ‏`RequestsApiService` הוא המקום היחיד שמכיר HTTP. ‏`RequestsPageComponent` מחזיק את ה-State. הטבלה, המסננים, הדפדוף, הנתונים המסכמים ופס העדכון המרוכז הם רכיבי תצוגה עם `input()`/`output()` בלבד.

## החלטות טכנולוגיות וחלופות

1. **Optimistic Concurrency עם `rowversion` של SQL Server.**
   *חלופות שנשקלו:* (א) Pessimistic Locking (`UPDATE … WITH (UPDLOCK)` או נעילה לוגית "בעריכה"): מחזיק נעילות לאורך זמן החשיבה של המשתמש, וגורם לחסימות ולנעילות "יתומות". (ב) עמודת `Version int` שמקודמת ידנית: עובדת גם ב-DB אחרים, אבל דורשת לזכור לקדם אותה בכל נתיב עדכון. `rowversion` מתעדכן אוטומטית ע״י ה-DB, כך שאי אפשר לשכוח.
2. **Offset Pagination עם `COUNT`.**
   *חלופה:* Keyset (Cursor) Pagination, מהיר יותר בעמודים עמוקים. נבחר Offset כי ה-UI דורש מספר עמודים וקפיצה לעמוד מסוים, ובעמודים הנפוצים (הראשונים) Offset מהיר בזכות האינדקסים. Keyset מופיע כהצעת שיפור ב-PERFORMANCE.md.
3. *(נוספת)* **שירותים ישירים מעל EF Core, בלי Repository/MediatR**: ראו "מבנה הפתרון".

## מגבלה ידועה ומה אפשר לשפר

- **מגבלה:** חיפוש טקסט חופשי (`LIKE '%x%'`) לא יכול להשתמש באינדקס. נמדד: כ-0.4 שניות לחיפוש, ועד כשנייה במונח נדיר, לעומת מילישניות בודדות לכל שאר הפעולות. ב-100K זה סביר, אבל זה לא יגדל טוב. שיפור: Full-Text Index (ראו PERFORMANCE.md).
- **מגבלה:** אין אימות משתמשים. `ChangedBy` מגיע מהלקוח (שדה "משתמש נוכחי" במסך). בסביבה אמיתית הוא יילקח מה-Claims של משתמש מאומת, ולא מגוף הבקשה.
- **שיפור:** Cache מבוזר (Redis) לפריסה במספר מופעים, ו-Keyset pagination לגלילה עמוקה.

## שימוש בכלי AI

במהלך הפיתוח השתמשתי ב-Claude Code (של Anthropic) ככלי עזר לכתיבת קוד.

**התפקיד שלי:** הובלתי את הפיתוח לאורך כל הדרך. הגדרתי את הדרישות, תכננתי את מבנה הפתרון והשכבות, קבעתי את מוסכמות הקוד והשמות, וקיבלתי את ההחלטות הטכניות. סקרתי את הקוד באופן שוטף, ביצעתי בו שינויים ותיקונים רבים, ובדקתי את המימוש מול כל סעיף באפיון עד שעמד בדרישות.

**אימות ובקרה:** הרצתי את המערכת ובדקתי את כל התרחישים בממשק, הרצתי את הבדיקות האוטומטיות ואת מדידות הביצועים, ווידאתי שהתיעוד ותוכנית העבודה משקפים את המימוש בפועל.

**השימוש ב-AI:** כתיבת קוד לפי ההנחיות שלי, הרצת בדיקות, וניסוח טיוטות לתיעוד, שאותן ערכתי ובדקתי.

אני מכירה את הקוד לעומק, יכולה להסביר כל רכיב וכל החלטה, והאחריות על התוצר היא שלי.

</div>
