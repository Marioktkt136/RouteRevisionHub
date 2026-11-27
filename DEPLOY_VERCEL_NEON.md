# دليل رفع `RouteRevisionHub.Online` على Vercel مع PostgreSQL (Neon)

التطبيق جاهز الآن للرفع على **Vercel** كحاوية `.NET 10` عبر **`Dockerfile.vercel`** مع دعم تلقائي لـ **PostgreSQL (Neon / Supabase)** عبر `DATABASE_URL` أو `POSTGRES_URL` (مع استمرار عمل MS SQL Server محلياً بدون تغيير).

---

## 1. إنشاء قاعدة بيانات Neon PostgreSQL مجاناً داخل Vercel (أو من Neon)

### الطريقة الأسهل (Vercel Marketplace):
1. ادخل على **Vercel Dashboard** -> **Integrations** -> ابحث عن **Neon Postgres** (أو **Supabase**) واضغط **Add Integration** ثم اربطه بمشروعك.
2. سيقوم Vercel تلقائياً بحقن المتغير **`POSTGRES_URL`** / **`DATABASE_URL`** في المشروع.
   *(التطبيق يتعرف على رابط `postgres://...` أو `postgresql://...` تلقائياً، وينشئ كل الجداول والفهارس ومفاتيح الجلسات والبيانات عند أول تشغيل!).*

---

## 2. إضافة متغيرات Google OAuth في Vercel Project Settings

من داخل مشروعك على Vercel ادخل **Settings -> Environment Variables** وأضف (لـ Production وPreview):

- `Authentication__Google__ClientId`:
  `698382992997-tq4lo7na2e1sfojje6i40h7722um9s1n.apps.googleusercontent.com`
- `Authentication__Google__ClientSecret`:
  *(انسخ القيمة من جهازك بأمر `dotnet user-secrets list` داخل مجلد المشروع)*

> ملاحظة: لو ربطت Neon خارج Vercel Dashboard، أضف أيضاً متغير `DATABASE_URL` = رابط الاتصال `postgresql://...` من لوحة Neon.

---

## 3. الرفع على Vercel (`vercel --prod` أو عبر GitHub)

1. Vercel سيكتشف ملف [`Dockerfile.vercel`](file:///C:/Users/mnaiem/.gemini/antigravity/scratch/RouteRevisionHub.Online/Dockerfile.vercel) تلقائياً ويبني صورة `.NET 10` ويعمل Deploy.
2. عبر CLI من داخل مجلد [`RouteRevisionHub.Online`](file:///C:/Users/mnaiem/.gemini/antigravity/scratch/RouteRevisionHub.Online):
   ```powershell
   npm i -g vercel
   vercel login
   vercel --prod
   ```
   *(أو ارفع المجلد على Repo في GitHub واعمل Import في Vercel Dashboard).*

---

## 4. خطوة أخيرة بعد الحصول على رابط `.vercel.app`

بعد أول Deploy هيعطيك Vercel رابطاً مثل `https://route-revision-hub.vercel.app`:
1. ادخل على **Google Cloud Console -> Google Auth Platform -> Clients -> `RouteRevisionHub Online`**.
2. في **Authorized JavaScript origins** أضف:
   `https://route-revision-hub.vercel.app`
3. في **Authorized redirect URIs** أضف:
   `https://route-revision-hub.vercel.app/signin-google`
4. لحماية كل الطلاب: أضف إيميلات زمايلك في **Audience -> Test users** (أو اضغط **Publish App**). فقط من يمتلك صلاحية فتح مجلد الكورس على Google Drive سيدخل بنجاح!

> 💡 **نصيحة لمشاهدة الفيديوهات الكبيرة على خطة Hobby المجانية:**
> في نافذة الفيديو داخل الموقع استخدم تبويب **`☁️ Google Drive Player`** أو **`☁️ Open in Google Drive ↗`** عند مشاهدة الفيديوهات الطويلة لأنها تتصل بـ Google Drive مباشرة بدون استهلاك حد الـ 300 ثانية الخاص بـ Vercel Hobby.
