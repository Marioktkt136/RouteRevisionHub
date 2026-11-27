# دليل نشر وتشغيل النسخة الأونلاين (`RouteRevisionHub.Online`) مع Google Drive

تعتمد هذه النسخة على **التحقق الشخصي بصلاحية Google Drive لكل مستخدم (Per-User OAuth + Drive ACL Guard)**:
- لا يمكن لأي شخص دخول البورتال أو مشاهدة أي فيديو/ملف إلا إذا سجل الدخول بحساب **Google** يمتلك فعلاً صلاحية فتح مجلد الكورس على Google Drive (`Eng Hossam Mostafa`).
- يتم بث الفيديوهات والملفات عبر توكن المستخدم نفسه (مع دعم **HTTP 206 Range Seek** للانتقال إلى ثانية الـ OCR، أو التبديل إلى **Google Drive Player** المدمج).
- كل طالب مسجل يحتفظ بتقدمه (`TopicProgress`)، وقائمة المهام (`ChecklistTasks`)، ومواعيد ومحاولات الامتحانات (`UserExamStates` & `ExamSubmissions`) معزولة بالكامل في **MS SQL Server** حسب بريده الإلكتروني.

---

## 1. إعداد Google Cloud Console (لمرة واحدة مجاناً)

1. ادخل على [Google Cloud Console](https://console.cloud.google.com/) وأنشئ مشروعاً جديداً (مثلاً `RouteRevisionHub`).
2. من **APIs & Services -> Library**: فعّل **Google Drive API**.
3. من **APIs & Services -> OAuth consent screen**:
   - اختر **External** وأكمل اسم التطبيق والبريد الإلكتروني.
   - أضف Scope: `https://www.googleapis.com/auth/drive.readonly` + `openid` + `email` + `profile`.
   - طالما التطبيق في وضع **Testing** (بدون مراجعة Google)، أضف إيميلات طلاب الجروب في **Test users** (حتى 100 إيميل)، أو انشر التطبيق (Publish App).
4. من **APIs & Services -> Credentials -> Create Credentials -> OAuth client ID -> Web application**:
   - أضف في **Authorized redirect URIs**:
     - للتجربة المحلية: `http://localhost:5295/signin-google`
     - للاستضافة الأونلاين (HTTPS): `https://YOUR-DOMAIN.com/signin-google`
5. انسخ **`Client ID`** و **`Client Secret`**.

---

## 2. إعداد المفاتيح وقاعدة البيانات (MS SQL Server)

في الاستضافة (MonsterASP / SmarterASP / Azure App Service)، ضع الإعدادات عبر **Environment Variables** (يفضّل عدم رفع المفاتيح داخل Git):

- `ConnectionStrings__DefaultConnection`: رابط اتصال MS SQL Server السحابي الخاص بالاستضافة.
- `Authentication__Google__ClientId`: قيمة Google OAuth Client ID.
- `Authentication__Google__ClientSecret`: قيمة Google OAuth Client Secret.

أو للتجربة المحلية الآمنة عبر User Secrets:

```powershell
cd "C:\Users\mnaiem\.gemini\antigravity\scratch\RouteRevisionHub.Online"
dotnet user-secrets init
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_CLIENT_ID.apps.googleusercontent.com"
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_CLIENT_SECRET"
dotnet run
```

ثم افتح: `http://localhost:5295`

---

## 3. النشر على الاستضافة (`MonsterASP` / `SmarterASP` / `Azure`)

1. أمر بناء نسخة النشر:
   ```powershell
   dotnet publish -c Release -o ./publish
   ```
2. ارفع محتوى مجلد `publish` بالكامل (شاملاً مجلد `Data/Seed/drive_knowledge_base.json` ومجلد `wwwroot`).
3. عند أول تشغيل للتطبيق، سيقوم `DatabaseSeeder.SeedAsync()` تلقائياً بإنشاء الجداول عبر EF Core Migrations (`RouteRevisionHubOnlineDb`) وفهرسة **692 ملف Google Drive** و**61 توبك** و**12 امتحان** و**15 مهمة checklist** مع حفظ مفاتيح الجلسات (`DataProtectionKeys`) في SQL Server.

---

## 4. المجلدات المرتبطة في `GoogleDriveSettings:RootFolders`

| المجلد | الدور | Required للتحقق من الدخول |
|---|---|---|
| `Eng Hossam Mostafa` (`12IW01q8dydfRpajsqRAzSxSVwaxG9hNT`) | MainTrack | `true` |
| `Aya Ali` (`1-MlfXkB3jFx-FxiWQ7a28aQB-ibYwPBA`) | Workshop | `false` |
| `Menna Elashry` (`1mF1zpLezhfB_4QzSDKCjpzGTpMDTb2Xt`) | Workshop | `false` |
| `Assignments Explanation` (`1Qb_pC8-b0ShT45yUnCyjY8Ej89oWbTFz`) | Assignments | `false` |
