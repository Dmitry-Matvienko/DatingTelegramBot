using DatingBot.Application.Interfaces;
using DatingBot.Domain.Enums;

namespace DatingBot.Application.Services;

public class LocalizationService : ILocalizationService
{
    private static readonly Dictionary<string, Dictionary<AppLanguage, string>> Strings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["LanguagePrompt"] = new()
        {
            [AppLanguage.Russian] = "🌍 <b>Пожалуйста, выберите язык интерфейса:</b>",
            [AppLanguage.Ukrainian] = "🌍 <b>Будь ласка, оберіть мову інтерфейсу:</b>",
            [AppLanguage.English] = "🌍 <b>Please select your preferred language:</b>",
            [AppLanguage.Hindi] = "🌍 <b>कृपया अपनी पसंदीदा भाषा चुनें:</b>",
            [AppLanguage.Portuguese] = "🌍 <b>Por favor, selecione seu idioma preferido:</b>",
            [AppLanguage.Indonesian] = "🌍 <b>Silakan pilih bahasa antarmuka Anda:</b>"
        },
        ["LanguageChangedSuccess"] = new()
        {
            [AppLanguage.Russian] = "✅ Язык интерфейса успешно изменен на <b>Русский</b>.",
            [AppLanguage.Ukrainian] = "✅ Мову інтерфейсу успішно змінено на <b>Українську</b>.",
            [AppLanguage.English] = "✅ Interface language successfully changed to <b>English</b>.",
            [AppLanguage.Hindi] = "✅ इंटरफ़ेस भाषा सफलतापूर्वक <b>हिन्दी</b> में बदल दी गई है।",
            [AppLanguage.Portuguese] = "✅ Idioma alterado com sucesso para <b>Português</b>.",
            [AppLanguage.Indonesian] = "✅ Bahasa berhasil diubah ke <b>Bahasa Indonesia</b>."
        },
        ["WelcomeTitle"] = new()
        {
            [AppLanguage.Russian] = "👋 Привет! Добро пожаловать в бота знакомств <b>DatingBot</b>!\n\nДавай создадим твою анкету за пару простых шагов.",
            [AppLanguage.Ukrainian] = "👋 Привіт! Ласкаво просимо до бота знайомств <b>DatingBot</b>!\n\nДавай створимо твою анкету за пару простих кроків.",
            [AppLanguage.English] = "👋 Hello! Welcome to <b>DatingBot</b>!\n\nLet's create your profile in a few simple steps.",
            [AppLanguage.Hindi] = "👋 नमस्ते! <b>DatingBot</b> में आपका स्वागत है!\n\nआइए कुछ आसान चरणों में आपकी प्रोफ़ाइल बनाएं।",
            [AppLanguage.Portuguese] = "👋 Olá! Bem-vindo ao <b>DatingBot</b>!\n\nVamos criar seu perfil em alguns passos simples.",
            [AppLanguage.Indonesian] = "👋 Halo! Selamat datang di <b>DatingBot</b>!\n\nMari buat profil Anda dalam beberapa langkah mudah."
        },
        ["GenderPrompt"] = new()
        {
            [AppLanguage.Russian] = "🚻 <b>Укажите ваш пол:</b>",
            [AppLanguage.Ukrainian] = "🚻 <b>Вкажіть вашу стать:</b>",
            [AppLanguage.English] = "🚻 <b>Select your gender:</b>",
            [AppLanguage.Hindi] = "🚻 <b>अपना लिंग चुनें:</b>",
            [AppLanguage.Portuguese] = "🚻 <b>Selecione seu gênero:</b>",
            [AppLanguage.Indonesian] = "🚻 <b>Pilih jenis kelamin Anda:</b>"
        },
        ["TargetGenderPrompt"] = new()
        {
            [AppLanguage.Russian] = "🔍 <b>Кого вы ищете?</b>",
            [AppLanguage.Ukrainian] = "🔍 <b>Кого ви шукаєте?</b>",
            [AppLanguage.English] = "🔍 <b>Who are you looking for?</b>",
            [AppLanguage.Hindi] = "🔍 <b>आप किसे ढूंढ रहे हैं?</b>",
            [AppLanguage.Portuguese] = "🔍 <b>Quem você está procurando?</b>",
            [AppLanguage.Indonesian] = "🔍 <b>Siapa yang Anda cari?</b>"
        },
        ["Gender_Male"] = new()
        {
            [AppLanguage.Russian] = "Парень 👦",
            [AppLanguage.Ukrainian] = "Хлопець 👦",
            [AppLanguage.English] = "Guy 👦",
            [AppLanguage.Hindi] = "लड़का 👦",
            [AppLanguage.Portuguese] = "Rapaz 👦",
            [AppLanguage.Indonesian] = "Pria 👦"
        },
        ["Gender_Female"] = new()
        {
            [AppLanguage.Russian] = "Девушка 👧",
            [AppLanguage.Ukrainian] = "Дівчина 👧",
            [AppLanguage.English] = "Girl 👧",
            [AppLanguage.Hindi] = "लड़की 👧",
            [AppLanguage.Portuguese] = "Moça 👧",
            [AppLanguage.Indonesian] = "Wanita 👧"
        },
        ["TargetGender_Male"] = new()
        {
            [AppLanguage.Russian] = "Парня 👦",
            [AppLanguage.Ukrainian] = "Хлопця 👦",
            [AppLanguage.English] = "Guys 👦",
            [AppLanguage.Hindi] = "लड़के 👦",
            [AppLanguage.Portuguese] = "Rapazes 👦",
            [AppLanguage.Indonesian] = "Pria 👦"
        },
        ["TargetGender_Female"] = new()
        {
            [AppLanguage.Russian] = "Девушку 👧",
            [AppLanguage.Ukrainian] = "Дівчину 👧",
            [AppLanguage.English] = "Girls 👧",
            [AppLanguage.Hindi] = "लड़कियां 👧",
            [AppLanguage.Portuguese] = "Moças 👧",
            [AppLanguage.Indonesian] = "Wanita 👧"
        },
        ["TargetGender_All"] = new()
        {
            [AppLanguage.Russian] = "Всех 👥",
            [AppLanguage.Ukrainian] = "Усіх 👥",
            [AppLanguage.English] = "Everyone 👥",
            [AppLanguage.Hindi] = "सभी 👥",
            [AppLanguage.Portuguese] = "Todos 👥",
            [AppLanguage.Indonesian] = "Semua 👥"
        },
        ["NamePrompt"] = new()
        {
            [AppLanguage.Russian] = "✍️ <b>Как вас зовут?</b>\n\n<i>(Введите ваше имя текстом):</i>",
            [AppLanguage.Ukrainian] = "✍️ <b>Як вас звати?</b>\n\n<i>(Введіть ваше ім'я текстом):</i>",
            [AppLanguage.English] = "✍️ <b>What is your name?</b>\n\n<i>(Type your name):</i>",
            [AppLanguage.Hindi] = "✍️ <b>आपका नाम क्या है?</b>\n\n<i>(अपना नाम टाइप करें):</i>",
            [AppLanguage.Portuguese] = "✍️ <b>Qual é o seu nome?</b>\n\n<i>(Digite seu nome):</i>",
            [AppLanguage.Indonesian] = "✍️ <b>Siapa nama Anda?</b>\n\n<i>(Ketik nama Anda):</i>"
        },
        ["AgePrompt"] = new()
        {
            [AppLanguage.Russian] = "🎂 <b>Сколько вам лет?</b>\n\n<i>(Введите число от 10 до 100):</i>",
            [AppLanguage.Ukrainian] = "🎂 <b>Скільки вам років?</b>\n\n<i>(Введіть число від 10 до 100):</i>",
            [AppLanguage.English] = "🎂 <b>How old are you?</b>\n\n<i>(Enter a number from 10 to 100):</i>",
            [AppLanguage.Hindi] = "🎂 <b>आपकी उम्र क्या है?</b>\n\n<i>(10 से 100 के बीच संख्या दर्ज करें):</i>",
            [AppLanguage.Portuguese] = "🎂 <b>Quantos anos você tem?</b>\n\n<i>(Digite um número de 10 a 100):</i>",
            [AppLanguage.Indonesian] = "🎂 <b>Berapa usia Anda?</b>\n\n<i>(Masukkan angka dari 10 hingga 100):</i>"
        },
        ["CityPrompt"] = new()
        {
            [AppLanguage.Russian] = "📍 <b>Из какого вы города?</b>\n\n<i>(Напишите название города текстом):</i>",
            [AppLanguage.Ukrainian] = "📍 <b>З якого ви міста?</b>\n\n<i>(Напишіть назву міста текстом):</i>",
            [AppLanguage.English] = "📍 <b>What city are you in?</b>\n\n<i>(Type your city name):</i>",
            [AppLanguage.Hindi] = "📍 <b>आप किस शहर में हैं?</b>\n\n<i>(अपने शहर का नाम टाइप करें):</i>",
            [AppLanguage.Portuguese] = "📍 <b>De qual cidade você é?</b>\n\n<i>(Digite o nome da sua cidade):</i>",
            [AppLanguage.Indonesian] = "📍 <b>Di kota mana Anda tinggal?</b>\n\n<i>(Ketik nama kota Anda):</i>"
        },
        ["HeightPrompt"] = new()
        {
            [AppLanguage.Russian] = "📏 <b>Укажите ваш рост в сантиметрах:</b>\n\n<i>(Введите число от 100 до 250 или нажмите «Пропустить»):</i>",
            [AppLanguage.Ukrainian] = "📏 <b>Вкажіть ваш зріст у сантиметрах:</b>\n\n<i>(Введіть число від 100 до 250 або натисніть «Пропустити»):</i>",
            [AppLanguage.English] = "📏 <b>Enter your height in centimeters:</b>\n\n<i>(Enter a number between 100 and 250 or click \"Skip\"):</i>",
            [AppLanguage.Hindi] = "📏 <b>सेंटीमीटर में अपनी ऊंचाई दर्ज करें:</b>\n\n<i>(100 से 250 के बीच संख्या दर्ज करें या \"छोड़ें\" पर क्लिक करें):</i>",
            [AppLanguage.Portuguese] = "📏 <b>Informe sua altura em centímetros:</b>\n\n<i>(Digite um número de 100 a 250 ou clique em \"Pular\"):</i>",
            [AppLanguage.Indonesian] = "📏 <b>Masukkan tinggi badan Anda dalam sentimeter:</b>\n\n<i>(Masukkan angka dari 100 hingga 250 atau klik \"Lewati\"):</i>"
        },
        ["Btn_Skip"] = new()
        {
            [AppLanguage.Russian] = "⏩ Пропустить",
            [AppLanguage.Ukrainian] = "⏩ Пропустити",
            [AppLanguage.English] = "⏩ Skip",
            [AppLanguage.Hindi] = "⏩ छोड़ें",
            [AppLanguage.Portuguese] = "⏩ Pular",
            [AppLanguage.Indonesian] = "⏩ Lewati"
        },
        ["Btn_RemoveHeight"] = new()
        {
            [AppLanguage.Russian] = "🗑 Не указывать рост",
            [AppLanguage.Ukrainian] = "🗑 Не вказувати зріст",
            [AppLanguage.English] = "🗑 Do not specify height",
            [AppLanguage.Hindi] = "🗑 ऊंचाई न बताएं",
            [AppLanguage.Portuguese] = "🗑 Não especificar altura",
            [AppLanguage.Indonesian] = "🗑 Jangan cantumkan tinggi"
        },
        ["PhotoPrompt"] = new()
        {
            [AppLanguage.Russian] = "📸 <b>Отправьте вашу фотографию:</b>\n\n<i>(Пришлите фото прямо в чат):</i>",
            [AppLanguage.Ukrainian] = "📸 <b>Надішліть вашу фотографію:</b>\n\n<i>(Надішліть фото безпосередньо в чат):</i>",
            [AppLanguage.English] = "📸 <b>Send your photo:</b>\n\n<i>(Send a picture directly to the chat):</i>",
            [AppLanguage.Hindi] = "📸 <b>अपनी तस्वीर भेजें:</b>\n\n<i>(सीधे चैट में एक फोटो भेजें):</i>",
            [AppLanguage.Portuguese] = "📸 <b>Envie sua foto:</b>\n\n<i>(Envie uma foto diretamente no chat):</i>",
            [AppLanguage.Indonesian] = "📸 <b>Kirim foto Anda:</b>\n\n<i>(Kirim foto langsung di chat):</i>"
        },
        ["InterestsPrompt"] = new()
        {
            [AppLanguage.Russian] = "🏷 <b>Выберите ваши интересы:</b>\n\n<i>Нажимайте на кнопки, чтобы отметить интересующие вас темы, затем нажмите «Готово ✅»:</i>",
            [AppLanguage.Ukrainian] = "🏷 <b>Оберіть ваші інтереси:</b>\n\n<i>Натискайте на кнопки, щоб відзначити цікаві теми, потім натисніть «Готово ✅»:</i>",
            [AppLanguage.English] = "🏷 <b>Select your interests:</b>\n\n<i>Tap the buttons to select topics that interest you, then click \"Done ✅\":</i>",
            [AppLanguage.Hindi] = "🏷 <b>अपनी रुचियां चुनें:</b>\n\n<i>अपनी पसंदीदा रुचियों पर टैप करें, फिर \"पूर्ण ✅\" पर क्लिक करें:</i>",
            [AppLanguage.Portuguese] = "🏷 <b>Selecione seus interesses:</b>\n\n<i>Toque nos botões para escolher seus interesses e clique em \"Concluído ✅\":</i>",
            [AppLanguage.Indonesian] = "🏷 <b>Pilih minat Anda:</b>\n\n<i>Ketuk tombol untuk memilih minat Anda, lalu klik \"Selesai ✅\":</i>"
        },
        ["Btn_Done"] = new()
        {
            [AppLanguage.Russian] = "Готово ✅",
            [AppLanguage.Ukrainian] = "Готово ✅",
            [AppLanguage.English] = "Done ✅",
            [AppLanguage.Hindi] = "पूर्ण ✅",
            [AppLanguage.Portuguese] = "Concluído ✅",
            [AppLanguage.Indonesian] = "Selesai ✅"
        },
        ["Btn_Save"] = new()
        {
            [AppLanguage.Russian] = "💾 Сохранить",
            [AppLanguage.Ukrainian] = "💾 Зберегти",
            [AppLanguage.English] = "💾 Save",
            [AppLanguage.Hindi] = "💾 सहेजें",
            [AppLanguage.Portuguese] = "💾 Salvar",
            [AppLanguage.Indonesian] = "💾 Simpan"
        },
        ["TargetPrompt"] = new()
        {
            [AppLanguage.Russian] = "🎯 <b>Какова ваша цель знакомства?</b>\n\n<i>(Выберите категорию):</i>",
            [AppLanguage.Ukrainian] = "🎯 <b>Яка ваша мета знайомства?</b>\n\n<i>(Оберіть категорію):</i>",
            [AppLanguage.English] = "🎯 <b>What is your dating goal?</b>\n\n<i>(Select a category):</i>",
            [AppLanguage.Hindi] = "🎯 <b>आपका डेटिंग लक्ष्य क्या है?</b>\n\n<i>(एक श्रेणी चुनें):</i>",
            [AppLanguage.Portuguese] = "🎯 <b>Qual é o seu objetivo no aplicativo?</b>\n\n<i>(Selecione uma categoria):</i>",
            [AppLanguage.Indonesian] = "🎯 <b>Apa tujuan kencan Anda?</b>\n\n<i>(Pilih kategori):</i>"
        },
        ["Target_Friends"] = new()
        {
            [AppLanguage.Russian] = "👥 Общение и поиск друзей",
            [AppLanguage.Ukrainian] = "👥 Спілкування та пошук друзів",
            [AppLanguage.English] = "👥 Chat & make friends",
            [AppLanguage.Hindi] = "👥 बातचीत और दोस्त बनाना",
            [AppLanguage.Portuguese] = "👥 Conversar e fazer amigos",
            [AppLanguage.Indonesian] = "👥 Mengobrol & mencari teman"
        },
        ["Target_Relationship"] = new()
        {
            [AppLanguage.Russian] = "❤️ Общение и отношения",
            [AppLanguage.Ukrainian] = "❤️ Спілкування та стосунки",
            [AppLanguage.English] = "❤️ Dating & relationships",
            [AppLanguage.Hindi] = "❤️ डेटिंग और रिश्ते",
            [AppLanguage.Portuguese] = "❤️ Namoro e relacionamentos",
            [AppLanguage.Indonesian] = "❤️ Kencan & hubungan"
        },
        ["Target_AdultOnly"] = new()
        {
            [AppLanguage.Russian] = "🔞 18+",
            [AppLanguage.Ukrainian] = "🔞 18+",
            [AppLanguage.English] = "🔞 18+",
            [AppLanguage.Hindi] = "🔞 18+",
            [AppLanguage.Portuguese] = "🔞 18+",
            [AppLanguage.Indonesian] = "🔞 18+"
        },
        ["AiBioPrompt"] = new()
        {
            [AppLanguage.Russian] = "🧠 <b>Опиши себя:</b> какой ты человек, твои привычки, манеры, поведение и так далее.\n\n🔒 <i>Это сообщение никто не увидит. Оно будет использоваться для анализа нашим ИИ для подбора лучшего кандидата для вас.</i>",
            [AppLanguage.Ukrainian] = "🧠 <b>Опиши себе:</b> яка ти людина, твої звички, манери, поведінка тощо.\n\n🔒 <i>Це повідомлення ніхто не побачить. Воно використовується ШІ виключно для підбору ідеальної пари для вас.</i>",
            [AppLanguage.English] = "🧠 <b>Describe yourself:</b> what kind of person you are, your habits, behavior, personality traits, etc.\n\n🔒 <i>Nobody will see this text. It will only be analyzed privately by our AI to find your best matches.</i>",
            [AppLanguage.Hindi] = "🧠 <b>अपने बारे में बताएं:</b> आप किस तरह के व्यक्ति हैं, आपकी आदतें, व्यवहार, व्यक्तित्व आदि।\n\n🔒 <i>यह संदेश किसी को नहीं दिखाया जाएगा। इसका उपयोग केवल हमारे AI द्वारा आपके लिए सबसे उपयुक्त मैच खोजने के लिए किया जाएगा।</i>",
            [AppLanguage.Portuguese] = "🧠 <b>Descreva a si mesmo:</b> sua personalidade, seus habits, estilo de vida e comportamento.\n\n🔒 <i>Ninguém verá esse texto. Ele será analisado pela nossa IA apenas para encontrar as pessoas mais compatíveis com você.</i>",
            [AppLanguage.Indonesian] = "🧠 <b>Deskripsikan diri Anda:</b> kepribadian Anda, kebiasaan, perilaku, gaya hidup, dll.\n\n🔒 <i>Teks ini dirahasiakan dan tidak akan ditampilkan ke pengguna lain. Teks hanya dianalisis oleh AI untuk mencarikan pasangan terbaik bagi Anda.</i>"
        },
        ["Menu_Search"] = new()
        {
            [AppLanguage.Russian] = "🔍 Искать анкеты",
            [AppLanguage.Ukrainian] = "🔍 Шукати анкети",
            [AppLanguage.English] = "🔍 Search profiles",
            [AppLanguage.Hindi] = "🔍 प्रोफाइल खोजें",
            [AppLanguage.Portuguese] = "🔍 Procurar perfis",
            [AppLanguage.Indonesian] = "🔍 Cari profil"
        },
        ["Menu_Profile"] = new()
        {
            [AppLanguage.Russian] = "👤 Мой профиль",
            [AppLanguage.Ukrainian] = "👤 Мій профіль",
            [AppLanguage.English] = "👤 My Profile",
            [AppLanguage.Hindi] = "👤 मेरी प्रोफाइल",
            [AppLanguage.Portuguese] = "👤 Meu Perfil",
            [AppLanguage.Indonesian] = "👤 Profil Saya"
        },
        ["Menu_Language"] = new()
        {
            [AppLanguage.Russian] = "🌐 Язык",
            [AppLanguage.Ukrainian] = "🌐 Мова",
            [AppLanguage.English] = "🌐 Language",
            [AppLanguage.Hindi] = "🌐 भाषा",
            [AppLanguage.Portuguese] = "🌐 Idioma",
            [AppLanguage.Indonesian] = "🌐 Bahasa"
        },
        ["MainMenuGreeting"] = new()
        {
            [AppLanguage.Russian] = "🏠 <b>Главное меню DatingBot</b>\n\nИспользуйте кнопки внизу для навигации:\n• 👤 <b>Мой профиль</b> — просмотр и редактирование своей анкеты, фото, интересов и настроек\n• 🔍 <b>Искать анкеты</b> — поиск людей по вашим критериям и оценка анкет",
            [AppLanguage.Ukrainian] = "🏠 <b>Головне меню DatingBot</b>\n\nВикористовуйте кнопки внизу для навігації:\n• 👤 <b>Мій профіль</b> — перегляд та редагування своєї анкети, фото, інтересів і налаштувань\n• 🔍 <b>Шукати анкети</b> — пошук людей за вашими критеріями та оцінка анкет",
            [AppLanguage.English] = "🏠 <b>DatingBot Main Menu</b>\n\nUse the buttons below to navigate:\n• 👤 <b>My Profile</b> — view and edit your profile, photo, interests, and preferences\n• 🔍 <b>Search profiles</b> — discover people matching your criteria and rate profiles",
            [AppLanguage.Hindi] = "🏠 <b>DatingBot मुख्य मेनू</b>\n\nनेविगेट करने के लिए नीचे दिए गए बटनों का उपयोग करें:\n• 👤 <b>मेरी प्रोफाइल</b> — अपनी प्रोफाइल, फोटो, रुचियां और सेटिंग्स देखें और संपादित करें\n• 🔍 <b>प्रोफाइल खोजें</b> — अपनी प्राथमिकताओं के अनुसार लोगों को खोजें और रेट करें",
            [AppLanguage.Portuguese] = "🏠 <b>Menu Principal DatingBot</b>\n\nUse os botões abaixo para navegar:\n• 👤 <b>Meu Perfil</b> — visualize e edite seu perfil, foto, interesses e preferências\n• 🔍 <b>Procurar perfis</b> — descubra pessoas que combinam com você e avalie perfis",
            [AppLanguage.Indonesian] = "🏠 <b>Menu Utama DatingBot</b>\n\nGunakan tombol di bawah untuk navigasi:\n• 👤 <b>Profil Saya</b> — lihat dan edit profil, foto, minat, dan preferensi Anda\n• 🔍 <b>Cari profil</b> — temukan orang yang cocok dengan kriteria Anda dan beri penilaian"
        },
        ["Btn_MainMenu"] = new()
        {
            [AppLanguage.Russian] = "🏠 Главное меню",
            [AppLanguage.Ukrainian] = "🏠 Головне меню",
            [AppLanguage.English] = "🏠 Main Menu",
            [AppLanguage.Hindi] = "🏠 मुख्य मेनू",
            [AppLanguage.Portuguese] = "🏠 Menu Principal",
            [AppLanguage.Indonesian] = "🏠 Menu Utama"
        },
        ["Btn_Report"] = new()
        {
            [AppLanguage.Russian] = "🚨 Пожаловаться",
            [AppLanguage.Ukrainian] = "🚨 Поскаржитися",
            [AppLanguage.English] = "🚨 Report",
            [AppLanguage.Hindi] = "🚨 शिकायत करें",
            [AppLanguage.Portuguese] = "🚨 Denunciar",
            [AppLanguage.Indonesian] = "🚨 Laporkan"
        },
        ["Btn_SearchAgain"] = new()
        {
            [AppLanguage.Russian] = "🔄 Искать заново",
            [AppLanguage.Ukrainian] = "🔄 Шукати заново",
            [AppLanguage.English] = "🔄 Search again",
            [AppLanguage.Hindi] = "🔄 फिर से खोजें",
            [AppLanguage.Portuguese] = "🔄 Buscar novamente",
            [AppLanguage.Indonesian] = "🔄 Cari lagi"
        },
        ["Btn_Back"] = new()
        {
            [AppLanguage.Russian] = "◀️ Назад",
            [AppLanguage.Ukrainian] = "◀️ Назад",
            [AppLanguage.English] = "◀️ Back",
            [AppLanguage.Hindi] = "◀️ पीछे",
            [AppLanguage.Portuguese] = "◀️ Voltar",
            [AppLanguage.Indonesian] = "◀️ Kembali"
        },
        ["Btn_Cancel"] = new()
        {
            [AppLanguage.Russian] = "❌ Отмена",
            [AppLanguage.Ukrainian] = "❌ Скасувати",
            [AppLanguage.English] = "❌ Cancel",
            [AppLanguage.Hindi] = "❌ रद्द करें",
            [AppLanguage.Portuguese] = "❌ Cancelar",
            [AppLanguage.Indonesian] = "❌ Batal"
        },
        ["Label_City"] = new()
        {
            [AppLanguage.Russian] = "Город",
            [AppLanguage.Ukrainian] = "Місто",
            [AppLanguage.English] = "City",
            [AppLanguage.Hindi] = "शहर",
            [AppLanguage.Portuguese] = "Cidade",
            [AppLanguage.Indonesian] = "Kota"
        },
        ["Label_Height"] = new()
        {
            [AppLanguage.Russian] = "Рост",
            [AppLanguage.Ukrainian] = "Зріст",
            [AppLanguage.English] = "Height",
            [AppLanguage.Hindi] = "ऊंचाई",
            [AppLanguage.Portuguese] = "Altura",
            [AppLanguage.Indonesian] = "Tinggi"
        },
        ["Label_Gender"] = new()
        {
            [AppLanguage.Russian] = "Пол",
            [AppLanguage.Ukrainian] = "Стать",
            [AppLanguage.English] = "Gender",
            [AppLanguage.Hindi] = "लिंग",
            [AppLanguage.Portuguese] = "Gênero",
            [AppLanguage.Indonesian] = "Jenis kelamin"
        },
        ["Label_LookingFor"] = new()
        {
            [AppLanguage.Russian] = "Ищет",
            [AppLanguage.Ukrainian] = "Шукає",
            [AppLanguage.English] = "Looking for",
            [AppLanguage.Hindi] = "खोज रहा है",
            [AppLanguage.Portuguese] = "Procura",
            [AppLanguage.Indonesian] = "Mencari"
        },
        ["Label_Target"] = new()
        {
            [AppLanguage.Russian] = "Цель",
            [AppLanguage.Ukrainian] = "Мета",
            [AppLanguage.English] = "Goal",
            [AppLanguage.Hindi] = "लक्ष्य",
            [AppLanguage.Portuguese] = "Objetivo",
            [AppLanguage.Indonesian] = "Tujuan"
        },
        ["Label_Interests"] = new()
        {
            [AppLanguage.Russian] = "Интересы",
            [AppLanguage.Ukrainian] = "Інтереси",
            [AppLanguage.English] = "Interests",
            [AppLanguage.Hindi] = "रुचियां",
            [AppLanguage.Portuguese] = "Interesses",
            [AppLanguage.Indonesian] = "Minat"
        },
        ["Label_CommonInterests"] = new()
        {
            [AppLanguage.Russian] = "Общие интересы",
            [AppLanguage.Ukrainian] = "Спільні інтереси",
            [AppLanguage.English] = "Common interests",
            [AppLanguage.Hindi] = "समान रुचियां",
            [AppLanguage.Portuguese] = "Interesses em comum",
            [AppLanguage.Indonesian] = "Minat yang sama"
        },
        ["Label_OtherInterests"] = new()
        {
            [AppLanguage.Russian] = "Другие интересы",
            [AppLanguage.Ukrainian] = "Інші інтереси",
            [AppLanguage.English] = "Other interests",
            [AppLanguage.Hindi] = "अन्य रुचियां",
            [AppLanguage.Portuguese] = "Outros interesses",
            [AppLanguage.Indonesian] = "Minat lainnya"
        },
        ["Label_MyRating"] = new()
        {
            [AppLanguage.Russian] = "Мой средний рейтинг",
            [AppLanguage.Ukrainian] = "Мій середній рейтинг",
            [AppLanguage.English] = "My average rating",
            [AppLanguage.Hindi] = "मेरी औसत रेटिंग",
            [AppLanguage.Portuguese] = "Minha avaliação média",
            [AppLanguage.Indonesian] = "Nilai rata-rata saya"
        },
        ["Label_NoRatingsYet"] = new()
        {
            [AppLanguage.Russian] = "Пока нет оценок",
            [AppLanguage.Ukrainian] = "Поки немає оцінок",
            [AppLanguage.English] = "No ratings yet",
            [AppLanguage.Hindi] = "अभी तक कोई रेटिंग नहीं",
            [AppLanguage.Portuguese] = "Ainda sem avaliações",
            [AppLanguage.Indonesian] = "Belum ada penilaian"
        },
        ["Label_AiBioSecret"] = new()
        {
            [AppLanguage.Russian] = "🧠 <b>Скрытое описание для ИИ:</b>",
            [AppLanguage.Ukrainian] = "🧠 <b>Прихований опис для ШІ:</b>",
            [AppLanguage.English] = "🧠 <b>Hidden AI profile description:</b>",
            [AppLanguage.Hindi] = "🧠 <b>AI के लिए गुप्त विवरण:</b>",
            [AppLanguage.Portuguese] = "🧠 <b>Descrição privada para a IA:</b>",
            [AppLanguage.Indonesian] = "🧠 <b>Deskripsi rahasia untuk AI:</b>"
        },
        ["Badge_Ai"] = new()
        {
            [AppLanguage.Russian] = "✨ <i>Этот человек больше всего подходит вам на основе ИИ-анализа</i>",
            [AppLanguage.Ukrainian] = "✨ <i>Ця людина найбільше підходить вам на основі аналізу ШІ</i>",
            [AppLanguage.English] = "✨ <i>This person is your best match based on AI analysis</i>",
            [AppLanguage.Hindi] = "✨ <i>यह व्यक्ति AI विश्लेषण के आधार पर आपके लिए सबसे उपयुक्त मैच है</i>",
            [AppLanguage.Portuguese] = "✨ <i>Esta pessoa é o seu melhor match com base na análise da IA</i>",
            [AppLanguage.Indonesian] = "✨ <i>Orang ini paling cocok untuk Anda berdasarkan analisis AI</i>"
        },
        ["Badge_SameCity"] = new()
        {
            [AppLanguage.Russian] = "📍 <i>Собеседник из вашего города</i>",
            [AppLanguage.Ukrainian] = "📍 <i>Співрозмовник з вашого міста</i>",
            [AppLanguage.English] = "📍 <i>Person from your city</i>",
            [AppLanguage.Hindi] = "📍 <i>आपके शहर का व्यक्ति</i>",
            [AppLanguage.Portuguese] = "📍 <i>Pessoa da sua cidade</i>",
            [AppLanguage.Indonesian] = "📍 <i>Orang dari kota Anda</i>"
        },
        ["Badge_NearbyCityDistance"] = new()
        {
            [AppLanguage.Russian] = "🚗 <i>Ближайший собеседник из г. {0} ({1} км от вас)</i>",
            [AppLanguage.Ukrainian] = "🚗 <i>Найближчий співрозмовник з м. {0} ({1} км від вас)</i>",
            [AppLanguage.English] = "🚗 <i>Nearby person from {0} ({1} km away)</i>",
            [AppLanguage.Hindi] = "🚗 <i>{0} से निकटतम व्यक्ति (आपसे {1} किमी)</i>",
            [AppLanguage.Portuguese] = "🚗 <i>Pessoa mais próxima de {0} (a {1} km de você)</i>",
            [AppLanguage.Indonesian] = "🚗 <i>Orang terdekat dari {0} ({1} km dari Anda)</i>"
        },
        ["Badge_NearbyCity"] = new()
        {
            [AppLanguage.Russian] = "🚗 <i>Собеседник из г. {0}</i>",
            [AppLanguage.Ukrainian] = "🚗 <i>Співрозмовник з м. {0}</i>",
            [AppLanguage.English] = "🚗 <i>Person from {0}</i>",
            [AppLanguage.Hindi] = "🚗 <i>{0} से व्यक्ति</i>",
            [AppLanguage.Portuguese] = "🚗 <i>Pessoa de {0}</i>",
            [AppLanguage.Indonesian] = "🚗 <i>Orang dari {0}</i>"
        },
        ["SearchEmpty"] = new()
        {
            [AppLanguage.Russian] = "😔 <b>Пока что нет новых анкет по вашим критериям.</b>\n\nВы можете нажать <b>«🔄 Искать заново»</b>, чтобы снова просмотреть анкеты из вашего города, или зайти позже!",
            [AppLanguage.Ukrainian] = "😔 <b>Наразі немає нових анкет за вашими критеріями.</b>\n\nВи можете натиснути <b>«🔄 Шукати заново»</b>, щоб переглянути анкети ще раз, або повернутися пізніше!",
            [AppLanguage.English] = "😔 <b>No more profiles matching your criteria right now.</b>\n\nYou can click <b>«🔄 Search again»</b> to reset and view profiles in your city again, or check back later!",
            [AppLanguage.Hindi] = "😔 <b>फ़िलहाल आपके मानदंडों से मेल खाने वाले कोई और प्रोफ़ाइल नहीं हैं।</b>\n\nआप अपने शहर के प्रोफाइल फिर से देखने के लिए <b>«🔄 फिर से खोजें»</b> पर क्लिक कर सकते हैं!",
            [AppLanguage.Portuguese] = "😔 <b>Não há mais perfis disponíveis com seus critérios no momento.</b>\n\nVocê pode clicar em <b>«🔄 Buscar novamente»</b> para rever os perfis da sua cidade ou voltar mais tarde!",
            [AppLanguage.Indonesian] = "😔 <b>Belum ada profil baru yang sesuai dengan kriteria Anda saat ini.</b>\n\nAnda dapat menekan <b>«🔄 Cari lagi»</b> untuk melihat kembali profil di kota Anda, atau periksa lagi nanti!"
        },
        ["Notification_MutualMatch"] = new()
        {
            [AppLanguage.Russian] = "🎉 <b>У вас взаимная симпатия!</b>",
            [AppLanguage.Ukrainian] = "🎉 <b>У вас взаємна симпатія!</b>",
            [AppLanguage.English] = "🎉 <b>It's a mutual match!</b>",
            [AppLanguage.Hindi] = "🎉 <b>परस्पर मैच!</b>",
            [AppLanguage.Portuguese] = "🎉 <b>Vocês deram match mútuo!</b>",
            [AppLanguage.Indonesian] = "🎉 <b>Kalian saling cocok!</b>"
        },
        ["Notification_HighRating"] = new()
        {
            [AppLanguage.Russian] = "💌 <b>Вашу анкету оценили!</b>\n\nКто-то поставил вашей анкете <b>{0}/10</b> ⭐!",
            [AppLanguage.Ukrainian] = "💌 <b>Вашу анкету оценили!</b>\n\nХтось поставив вашій анкеті <b>{0}/10</b> ⭐!",
            [AppLanguage.English] = "💌 <b>Someone rated your profile!</b>\n\nYour profile received <b>{0}/10</b> ⭐!",
            [AppLanguage.Hindi] = "💌 <b>किसी ने आपकी प्रोफ़ाइल को रेटिंग दी!</b>\n\nआपकी प्रोफ़ाइल को <b>{0}/10</b> ⭐ मिले!",
            [AppLanguage.Portuguese] = "💌 <b>Alguém avaliou seu perfil!</b>\n\nSeu perfil recebeu uma nota de <b>{0}/10</b> ⭐!",
            [AppLanguage.Indonesian] = "💌 <b>Seseorang menilai profil Anda!</b>\n\nProfil Anda mendapat nilai <b>{0}/10</b> ⭐!"
        },
        ["Error_NameEmpty"] = new()
        {
            [AppLanguage.Russian] = "Имя не может быть пустым.",
            [AppLanguage.Ukrainian] = "Ім'я не може бути порожнім.",
            [AppLanguage.English] = "Name cannot be empty.",
            [AppLanguage.Hindi] = "नाम खाली नहीं हो सकता।",
            [AppLanguage.Portuguese] = "O nome não pode ficar em branco.",
            [AppLanguage.Indonesian] = "Nama tidak boleh kosong."
        },
        ["Error_NameMinLength"] = new()
        {
            [AppLanguage.Russian] = "Имя должно содержать минимум 2 символа.",
            [AppLanguage.Ukrainian] = "Ім'я має містити щонайменше 2 символи.",
            [AppLanguage.English] = "Name must contain at least 2 characters.",
            [AppLanguage.Hindi] = "नाम में कम से कम 2 अक्षर होने चाहिए।",
            [AppLanguage.Portuguese] = "O nome deve conter pelo menos 2 caracteres.",
            [AppLanguage.Indonesian] = "Nama harus berisi minimal 2 karakter."
        },
        ["Error_NameMaxLength"] = new()
        {
            [AppLanguage.Russian] = "Имя не должно превышать 50 символов.",
            [AppLanguage.Ukrainian] = "Ім'я не повинно перевищувати 50 символів.",
            [AppLanguage.English] = "Name cannot exceed 50 characters.",
            [AppLanguage.Hindi] = "नाम 50 अक्षरों से अधिक नहीं हो सकता।",
            [AppLanguage.Portuguese] = "O nome não pode exceder 50 caracteres.",
            [AppLanguage.Indonesian] = "Nama tidak boleh lebih dari 50 karakter."
        },
        ["Error_NameLetters"] = new()
        {
            [AppLanguage.Russian] = "Имя может содержать только буквы, пробелы и дефисы.",
            [AppLanguage.Ukrainian] = "Ім'я може містити лише літери, пробіли та дефіси.",
            [AppLanguage.English] = "Name can only contain letters, spaces, and hyphens.",
            [AppLanguage.Hindi] = "नाम में केवल अक्षर, रिक्त स्थान और हाइफ़न हो सकते हैं।",
            [AppLanguage.Portuguese] = "O nome pode conter apenas letras, espaços e hífens.",
            [AppLanguage.Indonesian] = "Nama hanya boleh berisi huruf, spasi, dan tanda hubung."
        },
        ["Error_AgeRange"] = new()
        {
            [AppLanguage.Russian] = "Сервис доступен для пользователей от 10 до 100 лет.",
            [AppLanguage.Ukrainian] = "Сервіс доступний для користувачів від 10 до 100 років.",
            [AppLanguage.English] = "Service is available for users aged 10 to 100 years.",
            [AppLanguage.Hindi] = "यह सेवा 10 से 100 वर्ष के उपयोगकर्ताओं के लिए उपलब्ध है।",
            [AppLanguage.Portuguese] = "O serviço está disponível para usuários de 10 a 100 anos.",
            [AppLanguage.Indonesian] = "Layanan ini tersedia untuk pengguna berusia 10 hingga 100 tahun."
        },
        ["Error_AgeNumber"] = new()
        {
            [AppLanguage.Russian] = "Пожалуйста, введите возраст числом (от 10 до 100):",
            [AppLanguage.Ukrainian] = "Будь ласка, введіть вік числом (від 10 до 100):",
            [AppLanguage.English] = "Please enter your age as a number (from 10 to 100):",
            [AppLanguage.Hindi] = "कृपया अपनी उम्र संख्या में दर्ज करें (10 से 100):",
            [AppLanguage.Portuguese] = "Por favor, digite sua idade em números (de 10 a 100):",
            [AppLanguage.Indonesian] = "Silakan masukkan usia Anda dalam bentuk angka (10 hingga 100):"
        },
        ["Error_CityEmpty"] = new()
        {
            [AppLanguage.Russian] = "Город не может быть пустым.",
            [AppLanguage.Ukrainian] = "Місто не може бути порожнім.",
            [AppLanguage.English] = "City cannot be empty.",
            [AppLanguage.Hindi] = "शहर खाली नहीं हो सकता।",
            [AppLanguage.Portuguese] = "A cidade não pode ficar em branco.",
            [AppLanguage.Indonesian] = "Kota tidak boleh kosong."
        },
        ["Error_CityMinLength"] = new()
        {
            [AppLanguage.Russian] = "Название города должно быть не короче 2 символов.",
            [AppLanguage.Ukrainian] = "Назва міста має бути не коротшою за 2 символи.",
            [AppLanguage.English] = "City name must be at least 2 characters.",
            [AppLanguage.Hindi] = "शहर का नाम कम से कम 2 अक्षरों का होना चाहिए।",
            [AppLanguage.Portuguese] = "O nome da cidade deve ter pelo menos 2 caracteres.",
            [AppLanguage.Indonesian] = "Nama kota harus terdiri dari minimal 2 karakter."
        },
        ["Error_CityMaxLength"] = new()
        {
            [AppLanguage.Russian] = "Название города не должно превышать 100 символов.",
            [AppLanguage.Ukrainian] = "Назва міста не повинна перевищувати 100 символів.",
            [AppLanguage.English] = "City name cannot exceed 100 characters.",
            [AppLanguage.Hindi] = "शहर का नाम 100 अक्षरों से अधिक नहीं हो सकता।",
            [AppLanguage.Portuguese] = "O nome da cidade não pode exceder 100 caracteres.",
            [AppLanguage.Indonesian] = "Nama kota tidak boleh lebih dari 100 karakter."
        },
        ["Error_CityLetters"] = new()
        {
            [AppLanguage.Russian] = "Название города должно состоять только из букв.",
            [AppLanguage.Ukrainian] = "Назва міста має складатися лише з літер.",
            [AppLanguage.English] = "City name must contain only letters.",
            [AppLanguage.Hindi] = "शहर के नाम में केवल अक्षर होने चाहिए।",
            [AppLanguage.Portuguese] = "O nome da cidade deve conter apenas letras.",
            [AppLanguage.Indonesian] = "Nama kota hanya boleh terdiri dari huruf."
        },
        ["Error_HeightRange"] = new()
        {
            [AppLanguage.Russian] = "Пожалуйста, укажите реальный рост в сантиметрах (от 100 до 250 см).",
            [AppLanguage.Ukrainian] = "Будь ласка, вкажіть реальний зріст у сантиметрах (від 100 до 250 см).",
            [AppLanguage.English] = "Please enter a realistic height in cm (from 100 to 250 cm).",
            [AppLanguage.Hindi] = "कृपया वास्तविक ऊंचाई सेमी में दर्ज करें (100 से 250 सेमी)।",
            [AppLanguage.Portuguese] = "Por favor, insira uma altura realista em cm (de 100 a 250 cm).",
            [AppLanguage.Indonesian] = "Harap masukkan tinggi badan yang realistis dalam cm (100 hingga 250 cm)."
        },
        ["Error_HeightNumber"] = new()
        {
            [AppLanguage.Russian] = "Введите рост числом в см (от 100 до 250) или нажмите «Пропустить»:",
            [AppLanguage.Ukrainian] = "Введіть зріст числом у см (від 100 до 250) або натисніть «Пропустити»:",
            [AppLanguage.English] = "Enter your height in cm (100 to 250) or click \"Skip\":",
            [AppLanguage.Hindi] = "सेमी में ऊंचाई दर्ज करें (100 से 250) या \"छोड़ें\" पर क्लिक करें:",
            [AppLanguage.Portuguese] = "Digite sua altura em cm (100 a 250) ou clique em \"Pular\":",
            [AppLanguage.Indonesian] = "Masukkan tinggi badan dalam cm (100 hingga 250) atau klik \"Lewati\":"
        },
        ["Error_PhotoRequired"] = new()
        {
            [AppLanguage.Russian] = "Пожалуйста, отправьте фотографию (как фото, а не файл):",
            [AppLanguage.Ukrainian] = "Будь ласка, надішліть фотографію (як фото, а не файл):",
            [AppLanguage.English] = "Please send a picture (as a photo, not a file):",
            [AppLanguage.Hindi] = "कृपया एक तस्वीर भेजें (फ़ाइल नहीं, बल्कि फ़ोटो के रूप में):",
            [AppLanguage.Portuguese] = "Por favor, envie uma foto (como imagem, não como arquivo):",
            [AppLanguage.Indonesian] = "Silakan kirim foto (sebagai foto, bukan dokumen/file):"
        },
        ["Error_AiBioEmpty"] = new()
        {
            [AppLanguage.Russian] = "Описание не должно быть пустым.",
            [AppLanguage.Ukrainian] = "Опис не повинен бути порожнім.",
            [AppLanguage.English] = "Description cannot be empty.",
            [AppLanguage.Hindi] = "विवरण खाली नहीं होना चाहिए।",
            [AppLanguage.Portuguese] = "A descrição não pode ficar vazia.",
            [AppLanguage.Indonesian] = "Deskripsi tidak boleh kosong."
        },
        ["Error_AiBioMinLength"] = new()
        {
            [AppLanguage.Russian] = "Пожалуйста, напишите чуть подробнее (минимум 5 символов).",
            [AppLanguage.Ukrainian] = "Будь ласка, напишіть трохи детальніше (мінімум 5 символів).",
            [AppLanguage.English] = "Please write a bit more (at least 5 characters).",
            [AppLanguage.Hindi] = "कृपया थोड़ा और विस्तार से लिखें (कम से कम 5 अक्षर)।",
            [AppLanguage.Portuguese] = "Por favor, escreva um pouco mais (mínimo de 5 caracteres).",
            [AppLanguage.Indonesian] = "Harap tulis sedikit lebih banyak (minimal 5 karakter)."
        },
        ["Error_AiBioMaxLength"] = new()
        {
            [AppLanguage.Russian] = "Описание не должно превышать 2000 символов.",
            [AppLanguage.Ukrainian] = "Опис не повинен перевищувати 2000 символів.",
            [AppLanguage.English] = "Description cannot exceed 2000 characters.",
            [AppLanguage.Hindi] = "विवरण 2000 अक्षरों से अधिक नहीं हो सकता।",
            [AppLanguage.Portuguese] = "A descrição não pode exceder 2000 caracteres.",
            [AppLanguage.Indonesian] = "Deskripsi tidak boleh lebih dari 2000 karakter."
        },
        ["Error_AdultOnlyUnder18"] = new()
        {
            [AppLanguage.Russian] = "Категория 18+ доступна только пользователям старше 18 лет.",
            [AppLanguage.Ukrainian] = "Категорія 18+ доступна лише користувачам від 18 років.",
            [AppLanguage.English] = "Category 18+ is only available to users 18 years and older.",
            [AppLanguage.Hindi] = "18+ श्रेणी केवल 18 वर्ष से अधिक आयु के उपयोगकर्ताओं के लिए उपलब्ध है।",
            [AppLanguage.Portuguese] = "A categoria 18+ está disponível apenas para maiores de 18 anos.",
            [AppLanguage.Indonesian] = "Kategori 18+ hanya tersedia untuk pengguna berusia 18 tahun ke atas."
        },
        ["Error_InterestsMin"] = new()
        {
            [AppLanguage.Russian] = "Пожалуйста, выберите хотя бы 1 интерес.",
            [AppLanguage.Ukrainian] = "Будь ласка, оберіть хоча б 1 інтерес.",
            [AppLanguage.English] = "Please select at least 1 interest.",
            [AppLanguage.Hindi] = "कृपया कम से कम 1 रुचि चुनें।",
            [AppLanguage.Portuguese] = "Por favor, selecione pelo menos 1 interesse.",
            [AppLanguage.Indonesian] = "Pilih setidaknya 1 minat."
        },
        ["Error_AgeMaxLessThanMin"] = new()
        {
            [AppLanguage.Russian] = "Максимальный возраст не может быть меньше минимального ({0}).",
            [AppLanguage.Ukrainian] = "Максимальний вік не може бути меншим за мінімальний ({0}).",
            [AppLanguage.English] = "Maximum age cannot be less than minimum age ({0}).",
            [AppLanguage.Hindi] = "अधिकतम आयु न्यूनतम आयु ({0}) से कम नहीं हो सकती।",
            [AppLanguage.Portuguese] = "A idade máxima não pode ser menor que a idade mínima ({0}).",
            [AppLanguage.Indonesian] = "Usia maksimum tidak boleh lebih kecil dari usia minimum ({0})."
        },
        ["Error_UserNotFound"] = new()
        {
            [AppLanguage.Russian] = "Пользователь не найден.",
            [AppLanguage.Ukrainian] = "Користувача не знайдено.",
            [AppLanguage.English] = "User not found.",
            [AppLanguage.Hindi] = "उपयोगकर्ता नहीं मिला।",
            [AppLanguage.Portuguese] = "Usuário não encontrado.",
            [AppLanguage.Indonesian] = "Pengguna tidak ditemukan."
        },
        ["Error_ProfileNotFound"] = new()
        {
            [AppLanguage.Russian] = "Профиль не найден.",
            [AppLanguage.Ukrainian] = "Профіль не знайдено.",
            [AppLanguage.English] = "Profile not found.",
            [AppLanguage.Hindi] = "प्रोफ़ाइल नहीं मिली।",
            [AppLanguage.Portuguese] = "Perfil não encontrado.",
            [AppLanguage.Indonesian] = "Profil tidak ditemukan."
        },
        ["City_DidYouMean"] = new()
        {
            [AppLanguage.Russian] = "🔍 Вы имели в виду: <b>г. {0}</b>?",
            [AppLanguage.Ukrainian] = "🔍 Ви мали на увазі: <b>м. {0}</b>?",
            [AppLanguage.English] = "🔍 Did you mean: <b>{0}</b>?",
            [AppLanguage.Hindi] = "🔍 क्या आपका मतलब था: <b>{0}</b>?",
            [AppLanguage.Portuguese] = "🔍 Você quis dizer: <b>{0}</b>?",
            [AppLanguage.Indonesian] = "🔍 Apakah maksud Anda: <b>{0}</b>?"
        },
        ["City_TypeManually"] = new()
        {
            [AppLanguage.Russian] = "Пожалуйста, введите город текстом:",
            [AppLanguage.Ukrainian] = "Будь ласка, введіть місто текстом:",
            [AppLanguage.English] = "Please type the city name:",
            [AppLanguage.Hindi] = "कृपया शहर का नाम लिखें:",
            [AppLanguage.Portuguese] = "Por favor, digite o nome da cidade:",
            [AppLanguage.Indonesian] = "Silakan ketik nama kota:"
        },
        ["City_SearchReset"] = new()
        {
            [AppLanguage.Russian] = "🔄 <b>Просмотры анкет в вашем городе сброшены!</b>\n\nПоиск начат заново.",
            [AppLanguage.Ukrainian] = "🔄 <b>Перегляди анкет у вашому місті скинуто!</b>\n\nПошук розпочато заново.",
            [AppLanguage.English] = "🔄 <b>Profiles history in your city has been reset!</b>\n\nSearch restarted.",
            [AppLanguage.Hindi] = "🔄 <b>आपके शहर के प्रोफाइल रीसेट हो गए हैं!</b>\n\nखोज फिर से शुरू हुई।",
            [AppLanguage.Portuguese] = "🔄 <b>O histórico de visualizações da sua cidade foi redefinido!</b>\n\nBusca reiniciada.",
            [AppLanguage.Indonesian] = "🔄 <b>Riwayat profil di kota Anda telah diatur ulang!</b>\n\nPencarian dimulai kembali."
        },
        ["Active_ProfileActive"] = new()
        {
            [AppLanguage.Russian] = "🚀 Ваша анкета активна! Нажмите «🔍 Искать анкеты», чтобы начать знакомиться.",
            [AppLanguage.Ukrainian] = "🚀 Ваша анкета активна! Натисніть «🔍 Шукати анкети», щоб почати знайомитися.",
            [AppLanguage.English] = "🚀 Your profile is active! Click \"🔍 Search profiles\" to start discovering people.",
            [AppLanguage.Hindi] = "🚀 आपकी प्रोफ़ाइल सक्रिय है! लोगों से मिलने के लिए \"🔍 प्रोफाइल खोजें\" पर क्लिक करें।",
            [AppLanguage.Portuguese] = "🚀 Seu perfil está ativo! Clique em \"🔍 Procurar perfis\" para começar a conhecer pessoas.",
            [AppLanguage.Indonesian] = "🚀 Profil Anda sudah aktif! Klik \"🔍 Cari profil\" untuk mulai mencari kenalan."
        },
        ["Search_MustCompleteProfile"] = new()
        {
            [AppLanguage.Russian] = "Сначала завершите создание анкеты для поиска:",
            [AppLanguage.Ukrainian] = "Спочатку завершіть створення анкети для пошуку:",
            [AppLanguage.English] = "Please complete your profile registration first:",
            [AppLanguage.Hindi] = "कृपया पहले अपनी प्रोफ़ाइल पंजीकरण पूरा करें:",
            [AppLanguage.Portuguese] = "Por favor, conclua seu cadastro antes de buscar:",
            [AppLanguage.Indonesian] = "Harap selesaikan pembuatan profil Anda terlebih dahulu:"
        },
        ["Report_SentAdmin"] = new()
        {
            [AppLanguage.Russian] = "✅ Ваша жалоба отправлена администрации.",
            [AppLanguage.Ukrainian] = "✅ Ваша скарга надіслана адміністрації.",
            [AppLanguage.English] = "✅ Your report has been submitted to the moderators.",
            [AppLanguage.Hindi] = "✅ आपकी शिकायत प्रशासन को भेज दी गई है।",
            [AppLanguage.Portuguese] = "✅ Sua denúncia foi enviada aos administradores.",
            [AppLanguage.Indonesian] = "✅ Laporan Anda telah dikirim ke pihak admin."
        },
        ["Btn_ShowWhoRated"] = new()
        {
            [AppLanguage.Russian] = "👀 Показать кто оценил",
            [AppLanguage.Ukrainian] = "👀 Показати хто оцінив",
            [AppLanguage.English] = "👀 Show who rated",
            [AppLanguage.Hindi] = "👀 देखें किसने रेट किया",
            [AppLanguage.Portuguese] = "👀 Ver quem avaliou",
            [AppLanguage.Indonesian] = "👀 Lihat siapa yang menilai"
        },
        ["Label_Name"] = new()
        {
            [AppLanguage.Russian] = "Имя",
            [AppLanguage.Ukrainian] = "Ім'я",
            [AppLanguage.English] = "Name",
            [AppLanguage.Hindi] = "नाम",
            [AppLanguage.Portuguese] = "Nome",
            [AppLanguage.Indonesian] = "Nama"
        },
        ["Label_Age"] = new()
        {
            [AppLanguage.Russian] = "Возраст",
            [AppLanguage.Ukrainian] = "Вік",
            [AppLanguage.English] = "Age",
            [AppLanguage.Hindi] = "उम्र",
            [AppLanguage.Portuguese] = "Idade",
            [AppLanguage.Indonesian] = "Usia"
        },
        ["Label_AgeFilters"] = new()
        {
            [AppLanguage.Russian] = "Фильтры по возрасту",
            [AppLanguage.Ukrainian] = "Фільтри за віком",
            [AppLanguage.English] = "Age preferences",
            [AppLanguage.Hindi] = "आयु प्राथमिकताएं",
            [AppLanguage.Portuguese] = "Preferências de idade",
            [AppLanguage.Indonesian] = "Preferensi usia"
        },
        ["Prompt_ClickButtonToEdit"] = new()
        {
            [AppLanguage.Russian] = "✏️ <i>Нажмите на кнопку ниже, чтобы изменить нужный параметр:</i>",
            [AppLanguage.Ukrainian] = "✏️ <i>Натисніть кнопку нижче, щоб змінити потрібний параметр:</i>",
            [AppLanguage.English] = "✏️ <i>Click a button below to edit any parameter:</i>",
            [AppLanguage.Hindi] = "✏️ <i>किसी भी पैरामीटर को बदलने के लिए नीचे दिए गए बटन पर क्लिक करें:</i>",
            [AppLanguage.Portuguese] = "✏️ <i>Clique em um botão abaixo para editar qualquer informação:</i>",
            [AppLanguage.Indonesian] = "✏️ <i>Klik tombol di bawah untuk mengubah informasi:</i>"
        },
        ["Prompt_SearchAgePreferences"] = new()
        {
            [AppLanguage.Russian] = "⚙️ <b>Параметры поиска по возрасту:</b>",
            [AppLanguage.Ukrainian] = "⚙️ <b>Параметри пошуку за віком:</b>",
            [AppLanguage.English] = "⚙️ <b>Search age preferences:</b>",
            [AppLanguage.Hindi] = "⚙️ <b>खोज के लिए आयु प्राथमिकताएं:</b>",
            [AppLanguage.Portuguese] = "⚙️ <b>Preferências de idade para busca:</b>",
            [AppLanguage.Indonesian] = "⚙️ <b>Preferensi usia pencarian:</b>"
        },
        ["Prompt_MinAge"] = new()
        {
            [AppLanguage.Russian] = "🔢 <b>Введите минимальный возраст (от 10 до 100):</b>",
            [AppLanguage.Ukrainian] = "🔢 <b>Введіть мінімальний вік (від 10 до 100):</b>",
            [AppLanguage.English] = "🔢 <b>Enter minimum age (from 10 to 100):</b>",
            [AppLanguage.Hindi] = "🔢 <b>न्यूनतम आयु दर्ज करें (10 से 100):</b>",
            [AppLanguage.Portuguese] = "🔢 <b>Digite a idade mínima (de 10 a 100):</b>",
            [AppLanguage.Indonesian] = "🔢 <b>Masukkan usia minimum (dari 10 hingga 100):</b>"
        },
        ["Prompt_MaxAge"] = new()
        {
            [AppLanguage.Russian] = "🔢 <b>Введите максимальный возраст (больше {0}):</b>",
            [AppLanguage.Ukrainian] = "🔢 <b>Введіть максимальний вік (більше {0}):</b>",
            [AppLanguage.English] = "🔢 <b>Enter maximum age (greater than {0}):</b>",
            [AppLanguage.Hindi] = "🔢 <b>अधिकतम आयु दर्ज करें ({0} से अधिक):</b>",
            [AppLanguage.Portuguese] = "🔢 <b>Digite a idade máxima (maior que {0}):</b>",
            [AppLanguage.Indonesian] = "🔢 <b>Masukkan usia maksimum (lebih dari {0}):</b>"
        },
        ["Prompt_ReportDetails"] = new()
        {
            [AppLanguage.Russian] = "✍️ <b>Опишите причину вашей жалобы:</b>",
            [AppLanguage.Ukrainian] = "✍️ <b>Опишіть причину вашої скарги:</b>",
            [AppLanguage.English] = "✍️ <b>Please describe the reason for your report:</b>",
            [AppLanguage.Hindi] = "✍️ <b>कृपया अपनी शिकायत का कारण बताएं:</b>",
            [AppLanguage.Portuguese] = "✍️ <b>Por favor, descreva o motivo da sua denúncia:</b>",
            [AppLanguage.Indonesian] = "✍️ <b>Silakan jelaskan alasan laporan Anda:</b>"
        },
        ["Btn_Photo"] = new()
        {
            [AppLanguage.Russian] = "📸 Фото",
            [AppLanguage.Ukrainian] = "📸 Фото",
            [AppLanguage.English] = "📸 Photo",
            [AppLanguage.Hindi] = "📸 फोटो",
            [AppLanguage.Portuguese] = "📸 Foto",
            [AppLanguage.Indonesian] = "📸 Foto"
        },
        ["Btn_Filters"] = new()
        {
            [AppLanguage.Russian] = "⚙️ Фильтры",
            [AppLanguage.Ukrainian] = "⚙️ Фільтри",
            [AppLanguage.English] = "⚙️ Filters",
            [AppLanguage.Hindi] = "⚙️ फ़िल्टर",
            [AppLanguage.Portuguese] = "⚙️ Filtros",
            [AppLanguage.Indonesian] = "⚙️ Filter"
        },
        ["Btn_AiBio"] = new()
        {
            [AppLanguage.Russian] = "🧠 Описание для ИИ",
            [AppLanguage.Ukrainian] = "🧠 Опис для ШІ",
            [AppLanguage.English] = "🧠 AI Bio",
            [AppLanguage.Hindi] = "🧠 AI विवरण",
            [AppLanguage.Portuguese] = "🧠 Descrição da IA",
            [AppLanguage.Indonesian] = "🧠 Deskripsi AI"
        },
        ["Btn_CustomAgeRange"] = new()
        {
            [AppLanguage.Russian] = "🔢 Свой диапазон",
            [AppLanguage.Ukrainian] = "🔢 Власний діапазон",
            [AppLanguage.English] = "🔢 Custom range",
            [AppLanguage.Hindi] = "🔢 कस्टम रेंज",
            [AppLanguage.Portuguese] = "🔢 Faixa personalizada",
            [AppLanguage.Indonesian] = "🔢 Rentang khusus"
        },
        ["ReportReason_18Plus"] = new()
        {
            [AppLanguage.Russian] = "🔞 18+ / Непристойный контент",
            [AppLanguage.Ukrainian] = "🔞 18+ / Непристойний вміст",
            [AppLanguage.English] = "🔞 18+ / Inappropriate content",
            [AppLanguage.Hindi] = "🔞 18+ / अनुपयुक्त सामग्री",
            [AppLanguage.Portuguese] = "🔞 18+ / Conteúdo impróprio",
            [AppLanguage.Indonesian] = "🔞 18+ / Konten tidak pantas"
        },
        ["ReportReason_Inappropriate"] = new()
        {
            [AppLanguage.Russian] = "📑 Фейк / Некорректная анкета",
            [AppLanguage.Ukrainian] = "📑 Фейк / Некоректна анкета",
            [AppLanguage.English] = "📑 Fake / Inappropriate profile",
            [AppLanguage.Hindi] = "📑 नकली / अनुचित प्रोफ़ाइल",
            [AppLanguage.Portuguese] = "📑 Perfil falso / Incorreto",
            [AppLanguage.Indonesian] = "📑 Profil palsu / Tidak sesuai"
        },
        ["ReportReason_Other"] = new()
        {
            [AppLanguage.Russian] = "❓ Другое",
            [AppLanguage.Ukrainian] = "❓ Інше",
            [AppLanguage.English] = "❓ Other",
            [AppLanguage.Hindi] = "❓ अन्य",
            [AppLanguage.Portuguese] = "❓ Outro",
            [AppLanguage.Indonesian] = "❓ Lainnya"
        },
        ["Notification_RatingScoreReceived"] = new()
        {
            [AppLanguage.Russian] = "💌 <b>Оценка: {0}/10 ⭐!</b>\n",
            [AppLanguage.Ukrainian] = "💌 <b>Оцінка: {0}/10 ⭐!</b>\n",
            [AppLanguage.English] = "💌 <b>Rating: {0}/10 ⭐!</b>\n",
            [AppLanguage.Hindi] = "💌 <b>रेटिंग: {0}/10 ⭐!</b>\n",
            [AppLanguage.Portuguese] = "💌 <b>Avaliação: {0}/10 ⭐!</b>\n",
            [AppLanguage.Indonesian] = "💌 <b>Penilaian: {0}/10 ⭐!</b>\n"
        },
        ["Notification_MutualScore"] = new()
        {
            [AppLanguage.Russian] = "⭐️ Ваша оценка: <b>{0}/10</b> ⭐\n",
            [AppLanguage.Ukrainian] = "⭐️ Ваша оцінка: <b>{0}/10</b> ⭐\n",
            [AppLanguage.English] = "⭐️ Your rating: <b>{0}/10</b> ⭐\n",
            [AppLanguage.Hindi] = "⭐️ आपकी रेटिंग: <b>{0}/10</b> ⭐\n",
            [AppLanguage.Portuguese] = "⭐️ Sua avaliação: <b>{0}/10</b> ⭐\n",
            [AppLanguage.Indonesian] = "⭐️ Nilai Anda: <b>{0}/10</b> ⭐\n"
        },
        ["Notification_MutualContact"] = new()
        {
            [AppLanguage.Russian] = "💬 <b>Контакты:</b> {0}\n",
            [AppLanguage.Ukrainian] = "💬 <b>Контакти:</b> {0}\n",
            [AppLanguage.English] = "💬 <b>Contact:</b> {0}\n",
            [AppLanguage.Hindi] = "💬 <b>संपर्क:</b> {0}\n",
            [AppLanguage.Portuguese] = "💬 <b>Contato:</b> {0}\n",
            [AppLanguage.Indonesian] = "💬 <b>Kontak:</b> {0}\n"
        },
        ["Notification_CanMessageUser"] = new()
        {
            [AppLanguage.Russian] = "💬 <b>Вы можете написать этому человеку:</b>",
            [AppLanguage.Ukrainian] = "💬 <b>Ви можете написати цій людині:</b>",
            [AppLanguage.English] = "💬 <b>You can write to this person:</b>",
            [AppLanguage.Hindi] = "💬 <b>आप इस व्यक्ति को संदेश भेज सकते हैं:</b>",
            [AppLanguage.Portuguese] = "💬 <b>Você pode escrever para esta pessoa:</b>",
            [AppLanguage.Indonesian] = "💬 <b>Anda dapat mengirim pesan ke orang ini:</b>"
        },
        ["Btn_SendMessage"] = new()
        {
            [AppLanguage.Russian] = "💬 Написать",
            [AppLanguage.Ukrainian] = "💬 Написати",
            [AppLanguage.English] = "💬 Message",
            [AppLanguage.Hindi] = "💬 संदेश भेजें",
            [AppLanguage.Portuguese] = "💬 Enviar mensagem",
            [AppLanguage.Indonesian] = "💬 Kirim pesan"
        },
        ["Btn_Greeting"] = new()
        {
            [AppLanguage.Russian] = "💬 Приветствие",
            [AppLanguage.Ukrainian] = "💬 Привітання",
            [AppLanguage.English] = "💬 Greeting",
            [AppLanguage.Hindi] = "💬 अभिवादन",
            [AppLanguage.Portuguese] = "💬 Saudação",
            [AppLanguage.Indonesian] = "💬 Salam"
        },
        ["Label_Greeting"] = new()
        {
            [AppLanguage.Russian] = "Приветствие",
            [AppLanguage.Ukrainian] = "Привітання",
            [AppLanguage.English] = "Greeting",
            [AppLanguage.Hindi] = "अभिवादन",
            [AppLanguage.Portuguese] = "Saudação",
            [AppLanguage.Indonesian] = "Salam"
        },
        ["GreetingPrompt"] = new()
        {
            [AppLanguage.Russian] = "💬 <b>Напишите ваше приветствие (статус):</b>\n\n<i>Оно будет отображаться в вашей анкете и видно всем пользователям при поиске (например: «Ищу людей для прогулок по парку»):</i>",
            [AppLanguage.Ukrainian] = "💬 <b>Напишіть ваше привітання (статус):</b>\n\n<i>Воно буде відображатися у вашій анкеті та видно всім користувачам під час пошуку (наприклад: «Шукаю людей для прогулянок парком»):</i>",
            [AppLanguage.English] = "💬 <b>Write your greeting (bio/status):</b>\n\n<i>It will be displayed on your profile and visible to all users during search (e.g. \"Looking for someone to take walks in the park with\"):</i>",
            [AppLanguage.Hindi] = "💬 <b>अपना अभिवादन (स्थिति/बायो) लिखें:</b>\n\n<i>यह आपकी प्रोफ़ाइल पर दिखेगा और खोज के दौरान सभी उपयोगकर्ताओं को दिखाई देगा (उदा. \"पार्क में टहलने के लिए लोगों की तलाश है\"):</i>",
            [AppLanguage.Portuguese] = "💬 <b>Escreva sua saudação (bio/status):</b>\n\n<i>Ela será exibida no seu perfil e visível para todos os usuários na busca (ex: \"Procurando alguém para passear no parque\"):</i>",
            [AppLanguage.Indonesian] = "💬 <b>Tulis pesan salam (bio/status) Anda:</b>\n\n<i>Pesan ini akan ditampilkan di profil Anda dan terlihat oleh semua pengguna saat mencari (misal: \"Mencari teman untuk jalan-jalan santai di taman\"):</i>"
        },
        ["Error_GreetingEmpty"] = new()
        {
            [AppLanguage.Russian] = "Приветствие не может быть пустым.",
            [AppLanguage.Ukrainian] = "Привітання не може бути порожнім.",
            [AppLanguage.English] = "Greeting cannot be empty.",
            [AppLanguage.Hindi] = "अभिवादन खाली नहीं हो सकता।",
            [AppLanguage.Portuguese] = "A saudação não pode ficar em branco.",
            [AppLanguage.Indonesian] = "Pesan salam tidak boleh kosong."
        },
        ["Error_GreetingMinLength"] = new()
        {
            [AppLanguage.Russian] = "Приветствие должно содержать минимум 2 символа.",
            [AppLanguage.Ukrainian] = "Привітання має містити щонайменше 2 символи.",
            [AppLanguage.English] = "Greeting must contain at least 2 characters.",
            [AppLanguage.Hindi] = "अभिवादन में कम से कम 2 अक्षर होने चाहिए।",
            [AppLanguage.Portuguese] = "A saudação deve conter pelo menos 2 caracteres.",
            [AppLanguage.Indonesian] = "Pesan salam harus berisi minimal 2 karakter."
        },
        ["Error_GreetingMaxLength"] = new()
        {
            [AppLanguage.Russian] = "Приветствие не должно превышать 300 символов.",
            [AppLanguage.Ukrainian] = "Привітання не повинно перевищувати 300 символів.",
            [AppLanguage.English] = "Greeting cannot exceed 300 characters.",
            [AppLanguage.Hindi] = "अभिवादन 300 अक्षरों से अधिक नहीं हो सकता।",
            [AppLanguage.Portuguese] = "A saudação não pode exceder 300 caracteres.",
            [AppLanguage.Indonesian] = "Pesan salam tidak boleh lebih dari 300 karakter."
        },
        ["Admin_Btn_BanUser"] = new()
        {
            [AppLanguage.Russian] = "🚫 Заблокировать пользователя",
            [AppLanguage.Ukrainian] = "🚫 Заблокувати користувача",
            [AppLanguage.English] = "🚫 Ban user",
            [AppLanguage.Hindi] = "🚫 उपयोगकर्ता को ब्लॉक करें",
            [AppLanguage.Portuguese] = "🚫 Bloquear usuário",
            [AppLanguage.Indonesian] = "🚫 Blokir pengguna"
        },
        ["Admin_Btn_DeleteProfile"] = new()
        {
            [AppLanguage.Russian] = "🗑 Удалить анкету",
            [AppLanguage.Ukrainian] = "🗑 Видалити анкету",
            [AppLanguage.English] = "🗑 Delete profile",
            [AppLanguage.Hindi] = "🗑 प्रोफ़ाइल हटाएं",
            [AppLanguage.Portuguese] = "🗑 Excluir perfil",
            [AppLanguage.Indonesian] = "🗑 Hapus profil"
        },
        ["Admin_Btn_Ignore"] = new()
        {
            [AppLanguage.Russian] = "👁 Проигнорировать",
            [AppLanguage.Ukrainian] = "👁 Проігнорувати",
            [AppLanguage.English] = "👁 Ignore",
            [AppLanguage.Hindi] = "👁 अनदेखा करें",
            [AppLanguage.Portuguese] = "👁 Ignorar",
            [AppLanguage.Indonesian] = "👁 Abaikan"
        },
        ["Notification_ReportResolved"] = new()
        {
            [AppLanguage.Russian] = "🛡 <b>Спасибо, ваша жалоба была обработана.</b>\n\nМы удалили анкету пользователя.\n\nСпасибо, что остаётесь с нами и делаете нас лучше. Удачного поиска! ✨",
            [AppLanguage.Ukrainian] = "🛡 <b>Дякуємо, вашу скаргу було оброблено.</b>\n\nМи видалили анкету користувача.\n\nДякуємо, що залишаєтеся з нами та робите нас кращими. Вдалого пошуку! ✨",
            [AppLanguage.English] = "🛡 <b>Thank you, your report has been processed.</b>\n\nWe have removed the user's profile.\n\nThank you for staying with us and helping us improve. Happy searching! ✨",
            [AppLanguage.Hindi] = "🛡 <b>धन्यवाद, आपकी शिकायत संसाधित कर दी गई है।</b>\n\nहमने उपयोगकर्ता की प्रोफ़ाइल हटा दी है।\n\nहमारे साथ बने रहने और हमें बेहतर बनाने के लिए धन्यवाद। आपकी खोज सफल हो! ✨",
            [AppLanguage.Portuguese] = "🛡 <b>Obrigado, sua denúncia foi processada.</b>\n\nRemovemos o perfil do usuário.\n\nObrigado por estar conosco e nos ajudar a melhorar. Boa sorte na busca! ✨",
            [AppLanguage.Indonesian] = "🛡 <b>Terima kasih, laporan Anda telah diproses.</b>\n\nKami telah menghapus profil pengguna tersebut.\n\nTerima kasih telah bersama kami dan membantu kami menjadi lebih baik. Selamat mencari! ✨"
        },
        ["Notification_ViolatorBanned"] = new()
        {
            [AppLanguage.Russian] = "⛔ <b>Ваша учетная запись заблокирована из-за нарушений правил.</b>",
            [AppLanguage.Ukrainian] = "⛔ <b>Ваш обліковий запис заблоковано через порушення правил.</b>",
            [AppLanguage.English] = "⛔ <b>Your account has been banned due to violations of our rules.</b>",
            [AppLanguage.Hindi] = "⛔ <b>नियमों के उल्लंघन के कारण आपका खाता ब्लॉक कर दिया गया है।</b>",
            [AppLanguage.Portuguese] = "⛔ <b>Sua conta foi banida por violação das regras.</b>",
            [AppLanguage.Indonesian] = "⛔ <b>Akun Anda telah diblokir karena melanggar aturan.</b>"
        },
        ["Notification_ViolatorProfileDeleted"] = new()
        {
            [AppLanguage.Russian] = "⚠️ <b>Ваша анкета удалена из-за нарушений правил использования бота.</b>\n\nПожалуйста, заполните анкету заново. В случае повторных нарушений ваша учетная запись будет заблокирована.",
            [AppLanguage.Ukrainian] = "⚠️ <b>Вашу анкету видалено через порушення правил використання бота.</b>\n\nБудь ласка, заповніть анкету заново. У разі повторних порушень ваш обліковий запис буде заблоковано.",
            [AppLanguage.English] = "⚠️ <b>Your profile has been deleted due to violations of the bot's rules.</b>\n\nPlease fill out your profile again. In case of repeated violations, your account will be banned.",
            [AppLanguage.Hindi] = "⚠️ <b>बॉट्स के नियमों के उल्लंघन के कारण आपकी प्रोफ़ाइल हटा दी गई है।</b>\n\nकृपया अपनी प्रोफ़ाइल फिर से भरें। बार-बार उल्लंघन करने पर आपका खाता ब्लॉक कर दिया जाएगा।",
            [AppLanguage.Portuguese] = "⚠️ <b>Seu perfil foi excluído por violação das regras do bot.</b>\n\nPor favor, preencha seu perfil novamente. Em caso de reincidência, sua conta será banida.",
            [AppLanguage.Indonesian] = "⚠️ <b>Profil Anda telah dihapus karena melanggar aturan bot.</b>\n\nSilakan isi profil Anda kembali. Jika terjadi pelanggaran berulang, akun Anda akan diblokir."
        },
        ["Admin_Decision_UserBanned"] = new()
        {
            [AppLanguage.Russian] = "🚫 <b>Решение: Пользователь заблокирован</b>",
            [AppLanguage.Ukrainian] = "🚫 <b>Рішення: Користувача заблоковано</b>",
            [AppLanguage.English] = "🚫 <b>Decision: User banned</b>",
            [AppLanguage.Hindi] = "🚫 <b>निर्णय: उपयोगकर्ता को ब्लॉक किया गया</b>",
            [AppLanguage.Portuguese] = "🚫 <b>Decisão: Usuário banido</b>",
            [AppLanguage.Indonesian] = "🚫 <b>Keputusan: Pengguna diblokir</b>"
        },
        ["Admin_Decision_ProfileDeleted"] = new()
        {
            [AppLanguage.Russian] = "🗑 <b>Решение: Анкета пользователя удалена</b>",
            [AppLanguage.Ukrainian] = "🗑 <b>Рішення: Анкету користувача видалено</b>",
            [AppLanguage.English] = "🗑 <b>Decision: User profile deleted</b>",
            [AppLanguage.Hindi] = "🗑 <b>निर्णय: उपयोगकर्ता की प्रोफ़ाइल हटाई गई</b>",
            [AppLanguage.Portuguese] = "🗑 <b>Decisão: Perfil do usuário excluído</b>",
            [AppLanguage.Indonesian] = "🗑 <b>Keputusan: Profil pengguna dihapus</b>"
        },
        ["Admin_Decision_ReportIgnored"] = new()
        {
            [AppLanguage.Russian] = "👁 <b>Решение: Жалоба проигнорирована</b>",
            [AppLanguage.Ukrainian] = "👁 <b>Рішення: Скаргу проігноровано</b>",
            [AppLanguage.English] = "👁 <b>Decision: Report ignored</b>",
            [AppLanguage.Hindi] = "👁 <b>निर्णय: शिकायत अनदेखी की गई</b>",
            [AppLanguage.Portuguese] = "👁 <b>Decisão: Denúncia ignorada</b>",
            [AppLanguage.Indonesian] = "👁 <b>Keputusan: Laporan diabaikan</b>"
        },
        ["Admin_Alert_AlreadyProcessed"] = new()
        {
            [AppLanguage.Russian] = "⚠️ Эта жалоба уже была обработана ранее.",
            [AppLanguage.Ukrainian] = "⚠️ Ця скарга вже була оброблена раніше.",
            [AppLanguage.English] = "⚠️ This report has already been processed.",
            [AppLanguage.Hindi] = "⚠️ यह शिकायत पहले ही संसाधित की जा चुकी है।",
            [AppLanguage.Portuguese] = "⚠️ Esta denúncia já foi processada anteriormente.",
            [AppLanguage.Indonesian] = "⚠️ Laporan ini sudah diproses sebelumnya."
        },
        ["Admin_Alert_NoAccess"] = new()
        {
            [AppLanguage.Russian] = "⛔ У вас нет прав администратора.",
            [AppLanguage.Ukrainian] = "⛔ У вас немає прав адміністратора.",
            [AppLanguage.English] = "⛔ You do not have administrator permissions.",
            [AppLanguage.Hindi] = "⛔ आपके पास व्यवस्थापक अधिकार नहीं हैं।",
            [AppLanguage.Portuguese] = "⛔ Você não tem permissões de administrador.",
            [AppLanguage.Indonesian] = "⛔ Anda tidak memiliki izin administrator."
        },
        ["Account_Banned"] = new()
        {
            [AppLanguage.Russian] = "⛔ <b>Ваш аккаунт заблокирован за нарушение правил сервиса.</b>",
            [AppLanguage.Ukrainian] = "⛔ <b>Ваш акаунт заблоковано за порушення правил сервісу.</b>",
            [AppLanguage.English] = "⛔ <b>Your account has been banned for violating the terms of service.</b>",
            [AppLanguage.Hindi] = "⛔ <b>सेवा की शर्तों के उल्लंघन के कारण आपका खाता ब्लॉक कर दिया गया है।</b>",
            [AppLanguage.Portuguese] = "⛔ <b>Sua conta foi banida por violação dos termos de serviço.</b>",
            [AppLanguage.Indonesian] = "⛔ <b>Akun Anda telah diblokir karena melanggar ketentuan layanan.</b>"
        },
        ["Btn_PayUnbanStars"] = new()
        {
            [AppLanguage.Russian] = "⭐ Разблокировать за {0} ⭐",
            [AppLanguage.Ukrainian] = "⭐ Розблокувати за {0} ⭐",
            [AppLanguage.English] = "⭐ Unban for {0} ⭐",
            [AppLanguage.Hindi] = "⭐ {0} ⭐ में अनब्लॉक करें",
            [AppLanguage.Portuguese] = "⭐ Desbloquear por {0} ⭐",
            [AppLanguage.Indonesian] = "⭐ Buka blokir seharga {0} ⭐"
        },
        ["Btn_PayUnban100Stars"] = new()
        {
            [AppLanguage.Russian] = "⭐ Разблокировать за {0} ⭐",
            [AppLanguage.Ukrainian] = "⭐ Розблокувати за {0} ⭐",
            [AppLanguage.English] = "⭐ Unban for {0} ⭐",
            [AppLanguage.Hindi] = "⭐ {0} ⭐ में अनब्लॉक करें",
            [AppLanguage.Portuguese] = "⭐ Desbloquear por {0} ⭐",
            [AppLanguage.Indonesian] = "⭐ Buka blokir seharga {0} ⭐"
        },
        ["Payment_Unban_Title"] = new()
        {
            [AppLanguage.Russian] = "Разблокировка аккаунта",
            [AppLanguage.Ukrainian] = "Розблокування акаунта",
            [AppLanguage.English] = "Account Unban",
            [AppLanguage.Hindi] = "खाता अनब्लॉक",
            [AppLanguage.Portuguese] = "Desbloqueio de Conta",
            [AppLanguage.Indonesian] = "Buka Blokir Akun"
        },
        ["Payment_Unban_Description"] = new()
        {
            [AppLanguage.Russian] = "Снятие блокировки и полное восстановление доступа к сервису DatingBot.",
            [AppLanguage.Ukrainian] = "Зняття блокування та повне відновлення доступу до сервісу DatingBot.",
            [AppLanguage.English] = "Lifting the ban and full restoration of access to DatingBot.",
            [AppLanguage.Hindi] = "प्रतिबंध हटाना और DatingBot सेवा तक पहुंच की पूरी बहाली।",
            [AppLanguage.Portuguese] = "Remoção do banimento e restauração completa do acesso ao DatingBot.",
            [AppLanguage.Indonesian] = "Membuka blokir dan memulihkan akses penuh ke layanan DatingBot."
        },
        ["Payment_Unban_PriceLabel"] = new()
        {
            [AppLanguage.Russian] = "Разбан (100 звёзд)",
            [AppLanguage.Ukrainian] = "Розбан (100 зірок)",
            [AppLanguage.English] = "Unban (100 Stars)",
            [AppLanguage.Hindi] = "अनब्लॉक (100 स्टार्स)",
            [AppLanguage.Portuguese] = "Desbloqueio (100 Estrelas)",
            [AppLanguage.Indonesian] = "Buka Blokir (100 Bintang)"
        },
        ["Notification_UnbanSuccessful"] = new()
        {
            [AppLanguage.Russian] = "🎉 <b>Оплата получена! Ваш аккаунт успешно разблокирован.</b>\n\nПриятных знакомств! Нажмите кнопку ниже для перехода в главное меню.",
            [AppLanguage.Ukrainian] = "🎉 <b>Оплату отримано! Ваш акаунт успішно розблоковано.</b>\n\nПриємних знайомств! Натисніть кнопку нижче для переходу в головне меню.",
            [AppLanguage.English] = "🎉 <b>Payment received! Your account has been successfully unbanned.</b>\n\nEnjoy dating! Tap the button below to go to the main menu.",
            [AppLanguage.Hindi] = "🎉 <b>भुगतान प्राप्त हुआ! आपका खाता सफलतापूर्वक अनब्लॉक कर दिया गया है।</b>\n\nशुभकामनाएं! मुख्य मेनू पर जाने के लिए नीचे दिए गए बटन पर टैप करें।",
            [AppLanguage.Portuguese] = "🎉 <b>Pagamento recebido! Sua conta foi desbloqueada com sucesso.</b>\n\nBons encontros! Toque no botão abaixo para ir ao menu principal.",
            [AppLanguage.Indonesian] = "🎉 <b>Pembayaran diterima! Akun Anda berhasil dibuka blokirnya.</b>\n\nSelamat berkenalan! Ketuk tombol di bawah untuk kembali ke menu utama."
        },
        ["Admin_Welcome"] = new()
        {
            [AppLanguage.Russian] = "👋 Здравствуйте, <b>Администратор</b>!\n\nИспользуйте кнопки внизу:\n• 👤 <b>Мой профиль</b> — панель управления, медиакит рекламодателям, рассылки и жалобы\n• 🔍 <b>Искать анкеты</b> — сквозной просмотр и модерация всех анкет базы",
            [AppLanguage.Ukrainian] = "👋 Вітаємо, <b>Адміністратор</b>!\n\nВикористовуйте кнопки внизу:\n• 👤 <b>Мій профіль</b> — панель керування, медіакіт рекламодавцям, розсилки та скарги\n• 🔍 <b>Шукати анкети</b> — наскрізний перегляд та модерація всіх анкет бази",
            [AppLanguage.English] = "👋 Hello, <b>Administrator</b>!\n\nUse the buttons below:\n• 👤 <b>My Profile</b> — admin dashboard, media kit for advertisers, broadcasts & reports\n• 🔍 <b>Search profiles</b> — browse and moderate all profiles in the database",
            [AppLanguage.Hindi] = "👋 नमस्ते, <b>व्यवस्थापक</b>!\n\nनीचे दिए गए बटनों का उपयोग करें:\n• 👤 <b>मेरी प्रोफाइल</b> — व्यवस्थापक पैनल, विज्ञापनदाता आंकड़े, प्रसारण और शिकायतें\n• 🔍 <b>प्रोफाइल खोजें</b> — सभी प्रोफाइल ब्राउज़ करें",
            [AppLanguage.Portuguese] = "👋 Olá, <b>Administrador</b>!\n\nUse os botões abaixo:\n• 👤 <b>Meu Perfil</b> — painel administrativo, estatísticas para anunciantes, transmissões e denúncias\n• 🔍 <b>Procurar perfis</b> — navegar e moderar todos os perfis do banco",
            [AppLanguage.Indonesian] = "👋 Halo, <b>Administrator</b>!\n\nGunakan tombol di bawah:\n• 👤 <b>Profil Saya</b> — dasbor admin, statistik pengiklan, siaran & laporan\n• 🔍 <b>Cari profil</b> — jelajahi dan moderasi semua profil dalam database"
        },
        ["Admin_Panel_Title"] = new()
        {
            [AppLanguage.Russian] = "👑 <b>Панель администратора DatingBot</b>\n\nВыберите интересующий вас раздел:",
            [AppLanguage.Ukrainian] = "👑 <b>Панель адміністратора DatingBot</b>\n\nОберіть потрібний розділ:",
            [AppLanguage.English] = "👑 <b>DatingBot Admin Panel</b>\n\nSelect a section:",
            [AppLanguage.Hindi] = "👑 <b>DatingBot व्यवस्थापक पैनल</b>\n\nएक अनुभाग चुनें:",
            [AppLanguage.Portuguese] = "👑 <b>Painel Administrativo DatingBot</b>\n\nSelecione uma seção:",
            [AppLanguage.Indonesian] = "👑 <b>Panel Admin DatingBot</b>\n\nPilih bagian:"
        },
        ["Admin_Btn_Stats"] = new()
        {
            [AppLanguage.Russian] = "📊 Статистика аудитории",
            [AppLanguage.Ukrainian] = "📊 Статистика аудиторії",
            [AppLanguage.English] = "📊 Audience Analytics",
            [AppLanguage.Hindi] = "📊 दर्शक सांख्यिकी",
            [AppLanguage.Portuguese] = "📊 Estatísticas do Público",
            [AppLanguage.Indonesian] = "📊 Statistik Audiens"
        },
        ["Admin_Btn_Broadcast"] = new()
        {
            [AppLanguage.Russian] = "📢 Рассылка рекламы",
            [AppLanguage.Ukrainian] = "📢 Розсилка реклами",
            [AppLanguage.English] = "📢 Ad Broadcast",
            [AppLanguage.Hindi] = "📢 विज्ञापन प्रसारण",
            [AppLanguage.Portuguese] = "📢 Transmissão de Anúncios",
            [AppLanguage.Indonesian] = "📢 Siaran Iklan"
        },
        ["Admin_Btn_Reports"] = new()
        {
            [AppLanguage.Russian] = "🚨 Жалобы ({0})",
            [AppLanguage.Ukrainian] = "🚨 Скарги ({0})",
            [AppLanguage.English] = "🚨 Reports ({0})",
            [AppLanguage.Hindi] = "🚨 शिकायतें ({0})",
            [AppLanguage.Portuguese] = "🚨 Denúncias ({0})",
            [AppLanguage.Indonesian] = "🚨 Laporan ({0})"
        },
        ["Admin_Btn_NoReports"] = new()
        {
            [AppLanguage.Russian] = "🚨 Жалобы (0)",
            [AppLanguage.Ukrainian] = "🚨 Скарги (0)",
            [AppLanguage.English] = "🚨 Reports (0)",
            [AppLanguage.Hindi] = "🚨 शिकायतें (0)",
            [AppLanguage.Portuguese] = "🚨 Denúncias (0)",
            [AppLanguage.Indonesian] = "🚨 Laporan (0)"
        },
        ["Admin_Btn_BackToPanel"] = new()
        {
            [AppLanguage.Russian] = "◀️ В панель управления",
            [AppLanguage.Ukrainian] = "◀️ До панелі керування",
            [AppLanguage.English] = "◀️ Back to Admin Panel",
            [AppLanguage.Hindi] = "◀️ व्यवस्थापक पैनल पर वापस",
            [AppLanguage.Portuguese] = "◀️ Voltar ao Painel",
            [AppLanguage.Indonesian] = "◀️ Kembali ke Panel Admin"
        },
        ["Admin_Stats_Btn_CitySearch"] = new()
        {
            [AppLanguage.Russian] = "🔎 Расчет охвата по городу",
            [AppLanguage.Ukrainian] = "🔎 Розрахунок охоплення по місту",
            [AppLanguage.English] = "🔎 City Reach Calculator",
            [AppLanguage.Hindi] = "🔎 शहर द्वारा रीच कैलकुलेटर",
            [AppLanguage.Portuguese] = "🔎 Calculadora de Alcance por Cidade",
            [AppLanguage.Indonesian] = "🔎 Kalkulator Jangkauan Kota"
        },
        ["Admin_Stats_CityPrompt"] = new()
        {
            [AppLanguage.Russian] = "📍 <b>Введите название города:</b>\n\n<i>Напишите название города для расчета точных цифр охвата рекламодателю (например: Москва, Киев, New York):</i>",
            [AppLanguage.Ukrainian] = "📍 <b>Введіть назву міста:</b>\n\n<i>Напишіть назву міста для розрахунку точних цифр охоплення рекламодавцю (наприклад: Київ, Львів, New York):</i>",
            [AppLanguage.English] = "📍 <b>Enter city name:</b>\n\n<i>Type city name to calculate audience reach for advertisers (e.g. London, New York, Kyiv):</i>",
            [AppLanguage.Hindi] = "📍 <b>शहर का नाम दर्ज करें:</b>\n\n<i>विज्ञापनदाता के लिए दर्शक संख्या की गणना करने के लिए शहर का नाम टाइप करें:</i>",
            [AppLanguage.Portuguese] = "📍 <b>Digite o nome da cidade:</b>\n\n<i>Digite o nome da cidade para calcular o alcance do público para os anunciantes:</i>",
            [AppLanguage.Indonesian] = "📍 <b>Masukkan nama kota:</b>\n\n<i>Ketik nama kota untuk menghitung jangkauan audiens bagi pengiklan:</i>"
        },
        ["Admin_Stats_CityNotFound"] = new()
        {
            [AppLanguage.Russian] = "❌ В городе <b>{0}</b> пока нет зарегистрированных пользователей.",
            [AppLanguage.Ukrainian] = "❌ У місті <b>{0}</b> наразі немає зареєстрованих користувачів.",
            [AppLanguage.English] = "❌ No registered users found in <b>{0}</b>.",
            [AppLanguage.Hindi] = "❌ <b>{0}</b> में कोई पंजीकृत उपयोगकर्ता नहीं मिला।",
            [AppLanguage.Portuguese] = "❌ Nenhum usuário registrado encontrado em <b>{0}</b>.",
            [AppLanguage.Indonesian] = "❌ Tidak ada pengguna terdaftar yang ditemukan di <b>{0}</b>."
        },
        ["Admin_Broadcast_Menu"] = new()
        {
            [AppLanguage.Russian] = "📢 <b>Конструктор рассылки рекламы</b>\n\nВыберите аудиторию для рекламного сообщения:",
            [AppLanguage.Ukrainian] = "📢 <b>Конструктор розсилки реклами</b>\n\nОберіть аудиторію для рекламного повідомлення:",
            [AppLanguage.English] = "📢 <b>Ad Broadcast Constructor</b>\n\nSelect the target audience for your broadcast:",
            [AppLanguage.Hindi] = "📢 <b>विज्ञापन प्रसारण निर्माता</b>\n\nअपने प्रसारण के लिए लक्षित दर्शक चुनें:",
            [AppLanguage.Portuguese] = "📢 <b>Construtor de Transmissão de Anúncios</b>\n\nSelecione o público-alvo para o anúncio:",
            [AppLanguage.Indonesian] = "📢 <b>Pembuat Siaran Iklan</b>\n\nPilih audiens target untuk siaran:"
        },
        ["Admin_Broadcast_All"] = new()
        {
            [AppLanguage.Russian] = "📣 Всем пользователям",
            [AppLanguage.Ukrainian] = "📣 Усім користувачам",
            [AppLanguage.English] = "📣 All users",
            [AppLanguage.Hindi] = "📣 सभी उपयोगकर्ता",
            [AppLanguage.Portuguese] = "📣 Todos os usuários",
            [AppLanguage.Indonesian] = "📣 Semua pengguna"
        },
        ["Admin_Broadcast_Targeted"] = new()
        {
            [AppLanguage.Russian] = "🎯 С таргетингом (пол / город / возраст)",
            [AppLanguage.Ukrainian] = "🎯 З таргетингом (стать / місто / вік)",
            [AppLanguage.English] = "🎯 Targeted (gender / city / age)",
            [AppLanguage.Hindi] = "🎯 लक्षित (लिंग / शहर / आयु)",
            [AppLanguage.Portuguese] = "🎯 Segmentado (gênero / cidade / idade)",
            [AppLanguage.Indonesian] = "🎯 Bertarget (gender / kota / usia)"
        },
        ["Admin_Broadcast_Gender_Prompt"] = new()
        {
            [AppLanguage.Russian] = "🎯 <b>Выберите целевую аудиторию:</b>",
            [AppLanguage.Ukrainian] = "🎯 <b>Оберіть цільову аудиторію:</b>",
            [AppLanguage.English] = "🎯 <b>Select target audience:</b>",
            [AppLanguage.Hindi] = "🎯 <b>लक्षित दर्शक चुनें:</b>",
            [AppLanguage.Portuguese] = "🎯 <b>Selecione o público-alvo:</b>",
            [AppLanguage.Indonesian] = "🎯 <b>Pilih audiens target:</b>"
        },
        ["Admin_Broadcast_Gender_All"] = new()
        {
            [AppLanguage.Russian] = "👥 Всем пользователям",
            [AppLanguage.Ukrainian] = "👥 Усім користувачам",
            [AppLanguage.English] = "👥 All users",
            [AppLanguage.Hindi] = "👥 सभी उपयोगकर्ता",
            [AppLanguage.Portuguese] = "👥 Todos os usuários",
            [AppLanguage.Indonesian] = "👥 Semua pengguna"
        },
        ["Admin_Broadcast_Gender_Male"] = new()
        {
            [AppLanguage.Russian] = "👦 Только парни",
            [AppLanguage.Ukrainian] = "👦 Тільки хлопці",
            [AppLanguage.English] = "👦 Only guys",
            [AppLanguage.Hindi] = "👦 केवल लड़के",
            [AppLanguage.Portuguese] = "👦 Apenas rapazes",
            [AppLanguage.Indonesian] = "👦 Hanya pria"
        },
        ["Admin_Broadcast_Gender_Female"] = new()
        {
            [AppLanguage.Russian] = "👧 Только девушки",
            [AppLanguage.Ukrainian] = "👧 Тільки дівчата",
            [AppLanguage.English] = "👧 Only girls",
            [AppLanguage.Hindi] = "👧 केवल लड़कियां",
            [AppLanguage.Portuguese] = "👧 Apenas moças",
            [AppLanguage.Indonesian] = "👧 Hanya wanita"
        },
        ["Admin_Broadcast_Audience_Friends"] = new()
        {
            [AppLanguage.Russian] = "💬 Всем из категории общения",
            [AppLanguage.Ukrainian] = "💬 Усім з категорії спілкування",
            [AppLanguage.English] = "💬 Everyone in friendship category",
            [AppLanguage.Hindi] = "💬 दोस्ती श्रेणी के सभी लोग",
            [AppLanguage.Portuguese] = "💬 Todos da categoria amizade",
            [AppLanguage.Indonesian] = "💬 Semua dalam kategori pertemanan"
        },
        ["Admin_Broadcast_Audience_Relationship"] = new()
        {
            [AppLanguage.Russian] = "❤️ Всем из категории отношений",
            [AppLanguage.Ukrainian] = "❤️ Усім з категорії стосунків",
            [AppLanguage.English] = "❤️ Everyone in relationship category",
            [AppLanguage.Hindi] = "❤️ रिश्ते श्रेणी के सभी लोग",
            [AppLanguage.Portuguese] = "❤️ Todos da categoria relacionamento",
            [AppLanguage.Indonesian] = "❤️ Semua dalam kategori hubungan"
        },
        ["Admin_Broadcast_Audience_Adult"] = new()
        {
            [AppLanguage.Russian] = "🔞 Всем из категории 18+",
            [AppLanguage.Ukrainian] = "🔞 Усім з категорії 18+",
            [AppLanguage.English] = "🔞 Everyone in 18+ category",
            [AppLanguage.Hindi] = "🔞 18+ श्रेणी के सभी लोग",
            [AppLanguage.Portuguese] = "🔞 Todos da categoria 18+",
            [AppLanguage.Indonesian] = "🔞 Semua dalam kategori 18+"
        },
        ["Admin_Broadcast_City_Prompt"] = new()
        {
            [AppLanguage.Russian] = "📍 <b>Укажите город для таргетинга:</b>\n\n<i>Отправьте название города текстом или нажмите «Пропустить»:</i>",
            [AppLanguage.Ukrainian] = "📍 <b>Вкажіть місто для таргетингу:</b>\n\n<i>Надішліть назву міста текстом або натисніть «Пропустити»:</i>",
            [AppLanguage.English] = "📍 <b>Specify city for targeting:</b>\n\n<i>Type city name or click \"Skip\":</i>",
            [AppLanguage.Hindi] = "📍 <b>लक्ष्य के लिए शहर बताएं:</b>\n\n<i>शहर का नाम टाइप करें या \"छोड़ें\" पर क्लिक करें:</i>",
            [AppLanguage.Portuguese] = "📍 <b>Informe a cidade para segmentação:</b>\n\n<i>Digite o nome da cidade ou clique em \"Pular\":</i>",
            [AppLanguage.Indonesian] = "📍 <b>Tentukan kota untuk penargetan:</b>\n\n<i>Ketik nama kota atau klik \"Lewati\":</i>"
        },
        ["Admin_Broadcast_Content_Prompt"] = new()
        {
            [AppLanguage.Russian] = "✍️ <b>Отправьте рекламный пост:</b>\n\nПришлите текст сообщения или фотографию с текстом прямо в этот чат.",
            [AppLanguage.Ukrainian] = "✍️ <b>Надішліть рекламний пост:</b>\n\nНадішліть текст повідомлення або фотографію з текстом безпосередньо в цей чат.",
            [AppLanguage.English] = "✍️ <b>Send your ad message:</b>\n\nSend the text message or photo with caption directly to this chat.",
            [AppLanguage.Hindi] = "✍️ <b>अपना विज्ञापन पोस्ट भेजें:</b>\n\nसीधे इस चैट में टेक्स्ट संदेश या कैप्शन के साथ फोटो भेजें।",
            [AppLanguage.Portuguese] = "✍️ <b>Envie seu post publicitário:</b>\n\nEnvie a mensagem de texto ou foto com legenda diretamente neste chat.",
            [AppLanguage.Indonesian] = "✍️ <b>Kirim posting iklan Anda:</b>\n\nKirim pesan teks atau foto dengan keterangan langsung ke obrolan ini."
        },
        ["Admin_Broadcast_Button_Prompt"] = new()
        {
            [AppLanguage.Russian] = "🔗 <b>Инлайн-кнопка со ссылкой:</b>\n\nОтправьте текст кнопки и URL в формате:\n<code>Купить | https://example.com</code>\n\nили нажмите <b>«⏩ Пропустить»</b>, если кнопка не нужна.",
            [AppLanguage.Ukrainian] = "🔗 <b>Інлайн-кнопка з посиланням:</b>\n\nНадішліть текст кнопки та URL у форматі:\n<code>Купити | https://example.com</code>\n\nабо натисніть <b>«⏩ Пропустити»</b>, якщо кнопка не потрібна.",
            [AppLanguage.English] = "🔗 <b>Inline link button:</b>\n\nSend the button text and URL in this format:\n<code>Buy Now | https://example.com</code>\n\nor click <b>\"⏩ Skip\"</b> if not needed.",
            [AppLanguage.Hindi] = "🔗 <b>इनलाइन लिंक बटन:</b>\n\nइस प्रारूप में बटन टेक्स्ट और URL भेजें:\n<code>खरीदें | https://example.com</code>\n\nया यदि आवश्यकता न हो तो <b>\"⏩ छोड़ें\"</b> पर क्लिक करें।",
            [AppLanguage.Portuguese] = "🔗 <b>Botão com link inline:</b>\n\nEnvie o texto do botão e o URL no formato:\n<code>Comprar | https://example.com</code>\n\nou clique em <b>\"⏩ Pular\"</b> se não precisar.",
            [AppLanguage.Indonesian] = "🔗 <b>Tombol tautan inline:</b>\n\nKirim teks tombol dan URL dalam format:\n<code>Beli Sekarang | https://example.com</code>\n\natau klik <b>\"⏩ Lewati\"</b> jika tidak diperlukan."
        },
        ["Admin_Broadcast_Button_Invalid"] = new()
        {
            [AppLanguage.Russian] = "⚠️ Некорректный формат. Введите в формате <code>Текст | https://ссылка</code> или нажмите «⏩ Пропустить»:",
            [AppLanguage.Ukrainian] = "⚠️ Некоректний формат. Введіть у форматі <code>Текст | https://посилання</code> або натисніть «⏩ Пропустити»:",
            [AppLanguage.English] = "⚠️ Invalid format. Send as <code>Text | https://link</code> or click \"⏩ Skip\":",
            [AppLanguage.Hindi] = "⚠️ अमान्य प्रारूप। <code>टेक्स्ट | https://link</code> के रूप में भेजें या \"⏩ छोड़ें\" पर क्लिक करें:",
            [AppLanguage.Portuguese] = "⚠️ Formato inválido. Envie no formato <code>Texto | https://link</code> ou clique em \"⏩ Pular\":",
            [AppLanguage.Indonesian] = "⚠️ Format tidak valid. Kirim sebagai <code>Teks | https://link</code> atau klik \"⏩ Lewati\":"
        },
        ["Admin_Broadcast_Preview_Caption"] = new()
        {
            [AppLanguage.Russian] = "👁 <b>ПРЕДПРОСМОТР РЕКЛАМНОГО ПОСТА</b>\n\n🎯 <b>Целевой охват:</b> <b>{0}</b> пользователей\n\nЗапустить рассылку?",
            [AppLanguage.Ukrainian] = "👁 <b>ПОПЕРЕДНІЙ ПЕРЕГЛЯД РЕКЛАМНОГО ПОСТА</b>\n\n🎯 <b>Цільове охоплення:</b> <b>{0}</b> користувачів\n\nЗапустити розсилку?",
            [AppLanguage.English] = "👁 <b>AD BROADCAST PREVIEW</b>\n\n🎯 <b>Target Reach:</b> <b>{0}</b> users\n\nLaunch broadcast?",
            [AppLanguage.Hindi] = "👁 <b>विज्ञापन प्रसारण पूर्वावलोकन</b>\n\n🎯 <b>लक्षित पहुंच:</b> <b>{0}</b> उपयोगकर्ता\n\nप्रसारण शुरू करें?",
            [AppLanguage.Portuguese] = "👁 <b>PRÉ-VISUALIZAÇÃO DO ANÚNCIO</b>\n\n🎯 <b>Alcance estimado:</b> <b>{0}</b> usuários\n\nIniciar transmissão?",
            [AppLanguage.Indonesian] = "👁 <b>PRATINJAU SIARAN IKLAN</b>\n\n🎯 <b>Jangkauan Target:</b> <b>{0}</b> pengguna\n\nLuncurkan siaran?"
        },
        ["Admin_Broadcast_Btn_Send"] = new()
        {
            [AppLanguage.Russian] = "🚀 Отправить рассылку",
            [AppLanguage.Ukrainian] = "🚀 Надіслати розсилку",
            [AppLanguage.English] = "🚀 Send broadcast",
            [AppLanguage.Hindi] = "🚀 प्रसारण भेजें",
            [AppLanguage.Portuguese] = "🚀 Enviar transmissão",
            [AppLanguage.Indonesian] = "🚀 Kirim siaran"
        },
        ["Admin_Broadcast_Cancelled"] = new()
        {
            [AppLanguage.Russian] = "❌ Рассылка отменена.",
            [AppLanguage.Ukrainian] = "❌ Розсилку скасовано.",
            [AppLanguage.English] = "❌ Broadcast cancelled.",
            [AppLanguage.Hindi] = "❌ प्रसारण रद्द कर दिया गया।",
            [AppLanguage.Portuguese] = "❌ Transmissão cancelada.",
            [AppLanguage.Indonesian] = "❌ Siaran dibatalkan."
        },
        ["Admin_Broadcast_Progress"] = new()
        {
            [AppLanguage.Russian] = "⏳ Рассылка запущена... Отправка <b>{0}</b> пользователям.",
            [AppLanguage.Ukrainian] = "⏳ Розсилку запущено... Відправка <b>{0}</b> користувачам.",
            [AppLanguage.English] = "⏳ Broadcast started... Sending to <b>{0}</b> users.",
            [AppLanguage.Hindi] = "⏳ प्रसारण शुरू हुआ... <b>{0}</b> उपयोगकर्ताओं को भेजा जा रहा है।",
            [AppLanguage.Portuguese] = "⏳ Transmissão iniciada... Enviando para <b>{0}</b> usuários.",
            [AppLanguage.Indonesian] = "⏳ Siaran dimulai... Mengirim ke <b>{0}</b> pengguna."
        },
        ["Admin_Broadcast_Completed"] = new()
        {
            [AppLanguage.Russian] = "✅ <b>Рассылка рекламы завершена!</b>\n\n• 👥 Всего адресатов: <b>{0}</b>\n• 📨 Доставлено: <b>{1}</b>\n• 🚫 Ошибок / Заблокировали бота: <b>{2}</b>\n• ⏱ Затраченное время: <b>{3:F1} сек</b>",
            [AppLanguage.Ukrainian] = "✅ <b>Розсилку реклами завершено!</b>\n\n• 👥 Всього адресатів: <b>{0}</b>\n• 📨 Доставлено: <b>{1}</b>\n• 🚫 Помилок / Заблокували бота: <b>{2}</b>\n• ⏱ Витрачений час: <b>{3:F1} сек</b>",
            [AppLanguage.English] = "✅ <b>Ad broadcast completed!</b>\n\n• 👥 Total recipients: <b>{0}</b>\n• 📨 Delivered: <b>{1}</b>\n• 🚫 Errors / Blocked bot: <b>{2}</b>\n• ⏱ Time elapsed: <b>{3:F1} sec</b>",
            [AppLanguage.Hindi] = "✅ <b>विज्ञापन प्रसारण पूरा हुआ!</b>\n\n• 👥 कुल प्राप्तकर्ता: <b>{0}</b>\n• 📨 वितरित: <b>{1}</b>\n• 🚫 त्रुटियां / अवरुद्ध: <b>{2}</b>\n• ⏱ समय: <b>{3:F1} सेकंड</b>",
            [AppLanguage.Portuguese] = "✅ <b>Transmissão de anúncio concluída!</b>\n\n• 👥 Total de destinatários: <b>{0}</b>\n• 📨 Entregues: <b>{1}</b>\n• 🚫 Erros / Bloqueios: <b>{2}</b>\n• ⏱ Tempo: <b>{3:F1} seg</b>",
            [AppLanguage.Indonesian] = "✅ <b>Siaran iklan selesai!</b>\n\n• 👥 Total penerima: <b>{0}</b>\n• 📨 Terkirim: <b>{1}</b>\n• 🚫 Gagal / Diblokir: <b>{2}</b>\n• ⏱ Waktu: <b>{3:F1} dtk</b>"
        },
        ["Admin_Search_Gender_Prompt"] = new()
        {
            [AppLanguage.Russian] = "🔍 <b>Сквозной просмотр анкет</b>\n\nВыберите пол для просмотра всех анкет базы:",
            [AppLanguage.Ukrainian] = "🔍 <b>Наскрізний перегляд анкет</b>\n\nОберіть стать для перегляду всіх анкет бази:",
            [AppLanguage.English] = "🔍 <b>Admin Profile Search</b>\n\nSelect gender to browse all profiles in database:",
            [AppLanguage.Hindi] = "🔍 <b>व्यवस्थापक प्रोफाइल खोज</b>\n\nडेटाबेस में सभी प्रोफाइल ब्राउज़ करने के लिए लिंग चुनें:",
            [AppLanguage.Portuguese] = "🔍 <b>Visualização de Perfis (Admin)</b>\n\nSelecione o gênero para ver todos os perfis do banco:",
            [AppLanguage.Indonesian] = "🔍 <b>Pencarian Profil Admin</b>\n\nPilih jenis kelamin untuk melihat semua profil dalam database:"
        },
        ["Admin_Search_Btn_Block"] = new()
        {
            [AppLanguage.Russian] = "🚫 Заблокировать пользователя",
            [AppLanguage.Ukrainian] = "🚫 Заблокувати користувача",
            [AppLanguage.English] = "🚫 Block User",
            [AppLanguage.Hindi] = "🚫 उपयोगकर्ता को ब्लॉक करें",
            [AppLanguage.Portuguese] = "🚫 Bloquear Usuário",
            [AppLanguage.Indonesian] = "🚫 Blokir Pengguna"
        },
        ["Admin_Search_Btn_Delete"] = new()
        {
            [AppLanguage.Russian] = "🗑 Удалить анкету",
            [AppLanguage.Ukrainian] = "🗑 Видалити анкету",
            [AppLanguage.English] = "🗑 Delete Profile",
            [AppLanguage.Hindi] = "🗑 प्रोफाइल हटाएं",
            [AppLanguage.Portuguese] = "🗑 Excluir Perfil",
            [AppLanguage.Indonesian] = "🗑 Hapus Profil"
        },
        ["Admin_Search_Btn_Next"] = new()
        {
            [AppLanguage.Russian] = "➡️ Далее",
            [AppLanguage.Ukrainian] = "➡️ Далі",
            [AppLanguage.English] = "➡️ Next",
            [AppLanguage.Hindi] = "➡️ आगे",
            [AppLanguage.Portuguese] = "➡️ Próximo",
            [AppLanguage.Indonesian] = "➡️ Selanjutnya"
        },
        ["Admin_Search_Empty"] = new()
        {
            [AppLanguage.Russian] = "😔 В базе пока нет анкет выбранного пола.",
            [AppLanguage.Ukrainian] = "😔 У базі наразі немає анкет обраної статі.",
            [AppLanguage.English] = "😔 No profiles found for the selected gender in database.",
            [AppLanguage.Hindi] = "😔 डेटाबेस में चयनित लिंग के लिए कोई प्रोफ़ाइल नहीं मिली।",
            [AppLanguage.Portuguese] = "😔 Nenhum perfil encontrado para o gênero selecionado no banco.",
            [AppLanguage.Indonesian] = "😔 Tidak ada profil yang ditemukan untuk jenis kelamin yang dipilih."
        },
        ["Admin_Search_Blocked_Alert"] = new()
        {
            [AppLanguage.Russian] = "🚫 Пользователь заблокирован.",
            [AppLanguage.Ukrainian] = "🚫 Користувача заблоковано.",
            [AppLanguage.English] = "🚫 User has been banned.",
            [AppLanguage.Hindi] = "🚫 उपयोगकर्ता को ब्लॉक कर दिया गया है।",
            [AppLanguage.Portuguese] = "🚫 Usuário bloqueado.",
            [AppLanguage.Indonesian] = "🚫 Pengguna diblokir."
        },
        ["Admin_Search_Deleted_Alert"] = new()
        {
            [AppLanguage.Russian] = "🗑 Анкета пользователя удалена.",
            [AppLanguage.Ukrainian] = "🗑 Анкету користувача видалено.",
            [AppLanguage.English] = "🗑 User profile has been deleted.",
            [AppLanguage.Hindi] = "🗑 उपयोगकर्ता प्रोफ़ाइल हटा दी गई है।",
            [AppLanguage.Portuguese] = "🗑 Perfil do usuário excluído.",
            [AppLanguage.Indonesian] = "🗑 Profil pengguna dihapus."
        },
        ["Admin_Reports_NoPending"] = new()
        {
            [AppLanguage.Russian] = "👍 Необработанных жалоб нет!",
            [AppLanguage.Ukrainian] = "👍 Необроблених скарг немає!",
            [AppLanguage.English] = "👍 No pending reports!",
            [AppLanguage.Hindi] = "👍 कोई लंबित शिकायत नहीं है!",
            [AppLanguage.Portuguese] = "👍 Não há denúncias pendentes!",
            [AppLanguage.Indonesian] = "👍 Tidak ada laporan yang tertunda!"
        },
        ["Admin_Reports_Btn_NextReport"] = new()
        {
            [AppLanguage.Russian] = "➡️ Следующая жалоба",
            [AppLanguage.Ukrainian] = "➡️ Наступна скарга",
            [AppLanguage.English] = "➡️ Next Report",
            [AppLanguage.Hindi] = "➡️ अगली शिकायत",
            [AppLanguage.Portuguese] = "➡️ Próxima Denúncia",
            [AppLanguage.Indonesian] = "➡️ Laporan Berikutnya"
        },
        ["Admin_Alert_ErrorDeleteProfile"] = new()
        {
            [AppLanguage.Russian] = "⚠️ Ошибка удаления анкеты.",
            [AppLanguage.Ukrainian] = "⚠️ Помилка видалення анкети.",
            [AppLanguage.English] = "⚠️ Error deleting profile.",
            [AppLanguage.Hindi] = "⚠️ प्रोफ़ाइल हटाने में त्रुटि।",
            [AppLanguage.Portuguese] = "⚠️ Erro ao excluir perfil.",
            [AppLanguage.Indonesian] = "⚠️ Kesalahan saat menghapus profil."
        },
        ["Admin_Alert_ErrorBanUser"] = new()
        {
            [AppLanguage.Russian] = "⚠️ Ошибка блокировки пользователя.",
            [AppLanguage.Ukrainian] = "⚠️ Помилка блокування користувача.",
            [AppLanguage.English] = "⚠️ Error banning user.",
            [AppLanguage.Hindi] = "⚠️ उपयोगकर्ता को ब्लॉक करने में त्रुटि।",
            [AppLanguage.Portuguese] = "⚠️ Erro ao bloquear usuário.",
            [AppLanguage.Indonesian] = "⚠️ Kesalahan saat memblokir pengguna."
        },
        ["Admin_Btn_Revenue"] = new()
        {
            [AppLanguage.Russian] = "💰 Доход",
            [AppLanguage.Ukrainian] = "💰 Дохід",
            [AppLanguage.English] = "💰 Revenue",
            [AppLanguage.Hindi] = "💰 आय",
            [AppLanguage.Portuguese] = "💰 Receita",
            [AppLanguage.Indonesian] = "💰 Pendapatan"
        },
        ["Admin_Revenue_Menu"] = new()
        {
            [AppLanguage.Russian] = "💰 <b>Управление доходами и финансами</b>\n\nВыберите нужный раздел:",
            [AppLanguage.Ukrainian] = "💰 <b>Керування доходами та фінансами</b>\n\nОберіть потрібний розділ:",
            [AppLanguage.English] = "💰 <b>Revenue & Financial Management</b>\n\nSelect a section:",
            [AppLanguage.Hindi] = "💰 <b>राजस्व और वित्तीय प्रबंधन</b>\n\nएक अनुभाग चुनें:",
            [AppLanguage.Portuguese] = "💰 <b>Gestão de Receitas e Finanças</b>\n\nSelecione uma seção:",
            [AppLanguage.Indonesian] = "💰 <b>Manajemen Pendapatan & Keuangan</b>\n\nPilih bagian:"
        },
        ["Admin_Revenue_Btn_Balance"] = new()
        {
            [AppLanguage.Russian] = "💳 Баланс",
            [AppLanguage.Ukrainian] = "💳 Баланс",
            [AppLanguage.English] = "💳 Balance",
            [AppLanguage.Hindi] = "💳 शेष राशि",
            [AppLanguage.Portuguese] = "💳 Saldo",
            [AppLanguage.Indonesian] = "💳 Saldo"
        },
        ["Admin_Revenue_Btn_History"] = new()
        {
            [AppLanguage.Russian] = "📜 История транзакций",
            [AppLanguage.Ukrainian] = "📜 Історія транзакцій",
            [AppLanguage.English] = "📜 Transaction History",
            [AppLanguage.Hindi] = "📜 लेनदेन इतिहास",
            [AppLanguage.Portuguese] = "📜 Histórico de Transações",
            [AppLanguage.Indonesian] = "📜 Riwayat Transaksi"
        },
        ["Admin_Revenue_Balance_Report"] = new()
        {
            [AppLanguage.Russian] = "💳 <b>ФИНАНСОВЫЙ БАЛАНС БОТА</b>\n\n⭐️ <b>Всего заработано:</b> <code>{0}</code> ⭐ <i>(~${1:F2} USD)</i>\n🧾 <b>Всего транзакций:</b> <code>{2}</code>\n\n📈 <b>Динамика дохода:</b>\n• За последние 24 часа: <code>+{3}</code> ⭐\n• За последние 7 дней: <code>+{4}</code> ⭐\n• За последние 30 дней: <code>+{5}</code> ⭐\n\n<i>ℹ️ Средства поступают в Telegram Stars и доступны к выводу через Fragment.</i>",
            [AppLanguage.Ukrainian] = "💳 <b>ФІНАНСОВИЙ БАЛАНС БОТА</b>\n\n⭐️ <b>Всього зароблено:</b> <code>{0}</code> ⭐ <i>(~${1:F2} USD)</i>\n🧾 <b>Всього транзакцій:</b> <code>{2}</code>\n\n📈 <b>Динаміка доходу:</b>\n• За останні 24 години: <code>+{3}</code> ⭐\n• За останні 7 днів: <code>+{4}</code> ⭐\n• За останні 30 днів: <code>+{5}</code> ⭐\n\n<i>ℹ️ Кошти надходять у Telegram Stars і доступні для виведення через Fragment.</i>",
            [AppLanguage.English] = "💳 <b>BOT FINANCIAL BALANCE</b>\n\n⭐️ <b>Total Earned:</b> <code>{0}</code> ⭐ <i>(~${1:F2} USD)</i>\n🧾 <b>Total Transactions:</b> <code>{2}</code>\n\n📈 <b>Revenue Dynamics:</b>\n• Last 24 hours: <code>+{3}</code> ⭐\n• Last 7 days: <code>+{4}</code> ⭐\n• Last 30 days: <code>+{5}</code> ⭐\n\n<i>ℹ️ Funds are received in Telegram Stars and can be withdrawn via Fragment.</i>",
            [AppLanguage.Hindi] = "💳 <b>बॉट वित्तीय शेष</b>\n\n⭐️ <b>कुल कमाई:</b> <code>{0}</code> ⭐ <i>(~${1:F2} USD)</i>\n🧾 <b>कुल लेनदेन:</b> <code>{2}</code>\n\n📈 <b>राजस्व गतिशीलता:</b>\n• पिछले 24 घंटे: <code>+{3}</code> ⭐\n• पिछले 7 दिन: <code>+{4}</code> ⭐\n• पिछले 30 दिन: <code>+{5}</code> ⭐\n\n<i>ℹ️ फंड Telegram Stars में प्राप्त होते हैं और Fragment के माध्यम से निकाले जा सकते हैं।</i>",
            [AppLanguage.Portuguese] = "💳 <b>SALDO FINANCEIRO DO BOT</b>\n\n⭐️ <b>Total Ganho:</b> <code>{0}</code> ⭐ <i>(~${1:F2} USD)</i>\n🧾 <b>Total de Transações:</b> <code>{2}</code>\n\n📈 <b>Dinâmica de Receita:</b>\n• Últimas 24 horas: <code>+{3}</code> ⭐\n• Últimos 7 dias: <code>+{4}</code> ⭐\n• Últimos 30 dias: <code>+{5}</code> ⭐\n\n<i>ℹ️ Os fundos são recebidos em Telegram Stars e podem ser sacados via Fragment.</i>",
            [AppLanguage.Indonesian] = "💳 <b>SALDO KEUANGAN BOT</b>\n\n⭐️ <b>Total Pendapatan:</b> <code>{0}</code> ⭐ <i>(~${1:F2} USD)</i>\n🧾 <b>Total Transaksi:</b> <code>{2}</code>\n\n📈 <b>Dinamika Pendapatan:</b>\n• 24 jam terakhir: <code>+{3}</code> ⭐\n• 7 hari terakhir: <code>+{4}</code> ⭐\n• 30 hari terakhir: <code>+{5}</code> ⭐\n\n<i>ℹ️ Dana diterima dalam Telegram Stars dan dapat ditarik melalui Fragment.</i>"
        },
        ["Admin_Revenue_History_Header"] = new()
        {
            [AppLanguage.Russian] = "📜 <b>ИСТОРИЯ ТРАНЗАКЦИЙ (ПОСЛЕДНИЕ {0})</b>\n\n",
            [AppLanguage.Ukrainian] = "📜 <b>ІСТОРІЯ ТРАНЗАКЦІЙ (ОСТАННІ {0})</b>\n\n",
            [AppLanguage.English] = "📜 <b>TRANSACTION HISTORY (LAST {0})</b>\n\n",
            [AppLanguage.Hindi] = "📜 <b>लेनदेन इतिहास (अंतिम {0})</b>\n\n",
            [AppLanguage.Portuguese] = "📜 <b>HISTÓRICO DE TRANSAÇÕES (ÚLTIMAS {0})</b>\n\n",
            [AppLanguage.Indonesian] = "📜 <b>RIWAYAT TRANSAKSI ({0} TERAKHIR)</b>\n\n"
        },
        ["Admin_Revenue_NoTransactions"] = new()
        {
            [AppLanguage.Russian] = "<i>Пока нет совершенных транзакций.</i>",
            [AppLanguage.Ukrainian] = "<i>Наразі немає здійснених транзакцій.</i>",
            [AppLanguage.English] = "<i>No transactions completed yet.</i>",
            [AppLanguage.Hindi] = "<i>अभी तक कोई लेनदेन नहीं हुआ है।</i>",
            [AppLanguage.Portuguese] = "<i>Nenhuma transação realizada ainda.</i>",
            [AppLanguage.Indonesian] = "<i>Belum ada transaksi yang selesai.</i>"
        },
        ["Btn_Inactivity_StartSearch"] = new()
        {
            [AppLanguage.Russian] = "🔍 Начать поиск",
            [AppLanguage.Ukrainian] = "🔍 Почати пошук",
            [AppLanguage.English] = "🔍 Start searching",
            [AppLanguage.Hindi] = "🔍 खोजना शुरू करें",
            [AppLanguage.Portuguese] = "🔍 Começar a busca",
            [AppLanguage.Indonesian] = "🔍 Mulai mencari"
        },
        ["Notification_Inactivity_1"] = new()
        {
            [AppLanguage.Russian] = "🔥 <b>Кто-то прямо сейчас просматривает анкеты в твоем городе!</b>\n\nЗагляни в бот, возможно, тебя уже кто-то ждет!",
            [AppLanguage.Ukrainian] = "🔥 <b>Хтось просто зараз переглядає анкети у твоєму місті!</b>\n\nЗавітай у бот, можливо, на тебе вже хтось чекає!",
            [AppLanguage.English] = "🔥 <b>Someone is browsing profiles in your city right now!</b>\n\nCheck out the bot, maybe someone is already waiting for you!",
            [AppLanguage.Hindi] = "🔥 <b>कोई अभी आपके शहर में प्रोफाइल देख रहा है!</b>\n\nबॉट खोलें, शायद कोई आपका इंतज़ार कर रहा हो!",
            [AppLanguage.Portuguese] = "🔥 <b>Alguém está visualizando perfis na sua cidade agora mesmo!</b>\n\nDê uma olhada no bot, talvez alguém já esteja esperando por você!",
            [AppLanguage.Indonesian] = "🔥 <b>Seseorang sedang melihat-lihat profil di kotamu sekarang!</b>\n\nBuka bot, mungkin ada yang sedang menunggumu!"
        },
        ["Notification_Inactivity_2"] = new()
        {
            [AppLanguage.Russian] = "❤️ <b>Найди свою любовь!</b>\n\nНовые анкеты уже ждут твоей оценки. Сделай первый шаг!",
            [AppLanguage.Ukrainian] = "❤️ <b>Знайди своє кохання!</b>\n\nНові анкети вже чекають на твою оцінку. Зроби перший крок!",
            [AppLanguage.English] = "❤️ <b>Find your true love!</b>\n\nNew profiles are waiting for your rating. Take the first step!",
            [AppLanguage.Hindi] = "❤️ <b>अपना सच्चा प्यार पाएं!</b>\n\nनए प्रोफाइल आपकी रेटिंग का इंतज़ार कर रहे हैं। पहला कदम बढ़ाएं!",
            [AppLanguage.Portuguese] = "❤️ <b>Encontre o seu amor!</b>\n\nNovos perfis estão esperando pela sua avaliação. Dê o primeiro passo!",
            [AppLanguage.Indonesian] = "❤️ <b>Temukan cinta sejatimu!</b>\n\nProfil baru sedang menunggu penilaianmu. Ambil langkah pertama!"
        },
        ["Notification_Inactivity_3"] = new()
        {
            [AppLanguage.Russian] = "👥 <b>Ищешь новых друзей и интересное общение?</b>\n\nТысячи классных людей вокруг готовы познакомиться прямо сейчас!",
            [AppLanguage.Ukrainian] = "👥 <b>Шукаєш нових друзів та цікаве спілкування?</b>\n\nТисячі класних людей навколо готові познайомитися просто зараз!",
            [AppLanguage.English] = "👥 <b>Looking for new friends and engaging chats?</b>\n\nThousands of great people around you are ready to connect right now!",
            [AppLanguage.Hindi] = "👥 <b>नए दोस्तों और दिलचस्प बातचीत की तलाश है?</b>\n\nहजारों लोग आपसे जुड़ने के लिए तैयार हैं!",
            [AppLanguage.Portuguese] = "👥 <b>Procurando novos amigos e boas conversas?</b>\n\nMilhares de pessoas incríveis ao seu redor estão prontas para se conectar!",
            [AppLanguage.Indonesian] = "👥 <b>Mencari teman baru dan obrolan seru?</b>\n\nRibuan orang hebat di sekitarmu siap berkenalan sekarang!"
        },
        ["Notification_Inactivity_4"] = new()
        {
            [AppLanguage.Russian] = "💌 <b>С тобой хотят познакомиться!</b>\n\nНе упусти возможность завести приятное знакомство прямо сегодня.",
            [AppLanguage.Ukrainian] = "💌 <b>З тобою хочуть познайомитися!</b>\n\nНе втрачай можливість завести приємне знайомство просто сьогодні.",
            [AppLanguage.English] = "💌 <b>Someone wants to get to know you!</b>\n\nDon't miss the chance to start an exciting conversation today.",
            [AppLanguage.Hindi] = "💌 <b>कोई आपसे मिलना चाहता है!</b>\n\nआज ही एक सुखद परिचय शुरू करने का मौका न चूकें।",
            [AppLanguage.Portuguese] = "💌 <b>Alguém quer te conhecer!</b>\n\nNão perca a chance de iniciar uma conversa incrível hoje mesmo.",
            [AppLanguage.Indonesian] = "💌 <b>Ada yang ingin berkenalan denganmu!</b>\n\nJangan lewatkan kesempatan untuk memulai perkenalan yang menyenangkan hari ini."
        },
        ["Notification_Inactivity_5"] = new()
        {
            [AppLanguage.Russian] = "✨ <b>Твоя идеальная пара может быть совсем рядом!</b>\n\nНаш ИИ подобрал для тебя новые классные анкеты с высоким совпадением.",
            [AppLanguage.Ukrainian] = "✨ <b>Твоя ідеальна пара може бути зовсім поруч!</b>\n\nНаш ШІ підібрав для тебе нові класні анкети з високим збігом.",
            [AppLanguage.English] = "✨ <b>Your perfect match might be right around the corner!</b>\n\nOur AI has found great new profiles with high compatibility for you.",
            [AppLanguage.Hindi] = "✨ <b>आपका आदर्श साथी आपके बहुत करीब हो सकता है!</b>\n\nहमारे AI ने आपके लिए उच्च अनुकूलता वाले नए प्रोफाइल चुने हैं।",
            [AppLanguage.Portuguese] = "✨ <b>Seu par ideal pode estar muito perto de você!</b>\n\nNossa IA selecionou ótimos novos perfis com alta compatibilidade para você.",
            [AppLanguage.Indonesian] = "✨ <b>Pasangan idealmu mungkin ada di dekatmu!</b>\n\nAI kami telah memilih profil baru yang sangat cocok untukmu."
        },
        ["Notification_Inactivity_6"] = new()
        {
            [AppLanguage.Russian] = "💬 <b>Тебе скучно?</b>\n\nОткрой поиск анкет в DatingBot и начни увлекательный диалог прямо сейчас!",
            [AppLanguage.Ukrainian] = "💬 <b>Тобі нудно?</b>\n\nВідкрий пошук анкет у DatingBot та почни захоплюючий діалог просто зараз!",
            [AppLanguage.English] = "💬 <b>Feeling bored?</b>\n\nOpen profile search in DatingBot and start an exciting conversation right now!",
            [AppLanguage.Hindi] = "💬 <b>क्या आप बोर हो रहे हैं?</b>\n\nDatingBot में प्रोफाइल खोजें और अभी एक रोमांचक बातचीत शुरू करें!",
            [AppLanguage.Portuguese] = "💬 <b>Está entediado?</b>\n\nAbra a busca de perfis no DatingBot e comece um bate-papo incrível agora mesmo!",
            [AppLanguage.Indonesian] = "💬 <b>Merasa bosan?</b>\n\nBuka pencarian profil di DatingBot dan mulai obrolan seru sekarang juga!"
        },
        ["Notification_Inactivity_7"] = new()
        {
            [AppLanguage.Russian] = "🎯 <b>Твоя судьба в твоих руках!</b>\n\nЗайди в бот и оцени свежие анкеты людей поблизости.",
            [AppLanguage.Ukrainian] = "🎯 <b>Твоя доля у твоїх руках!</b>\n\nЗавітай у бот та оціни свіжі анкети людей поблизу.",
            [AppLanguage.English] = "🎯 <b>Your destiny is in your hands!</b>\n\nJump into the bot and check out fresh profiles of people near you.",
            [AppLanguage.Hindi] = "🎯 <b>आपकी किस्मत आपके हाथों में है!</b>\n\nबॉट में जाएं और अपने आस-पास के लोगों के नए प्रोफाइल देखें।",
            [AppLanguage.Portuguese] = "🎯 <b>O seu destino está em suas mãos!</b>\n\nEntre no bot e avalie perfis recentes de pessoas perto de você.",
            [AppLanguage.Indonesian] = "🎯 <b>Takdirmu ada di tanganmu!</b>\n\nBuka bot dan nilai profil terbaru dari orang-orang di dekatmu."
        },
        ["Notification_Inactivity_8"] = new()
        {
            [AppLanguage.Russian] = "🌟 <b>Кто-то ждет именно тебя!</b>\n\nПоставь оценку новым анкетам и узнай, совпали ли ваши симпатии.",
            [AppLanguage.Ukrainian] = "🌟 <b>Хтось чекає саме на тебе!</b>\n\nПостав оцінку новим анкетам і дізнайся, чи збіглися ваші симпатії.",
            [AppLanguage.English] = "🌟 <b>Someone is waiting just for you!</b>\n\nRate new profiles and see if you have a mutual match.",
            [AppLanguage.Hindi] = "🌟 <b>कोई सिर्फ आपका इंतज़ार कर रहा है!</b>\n\nनए प्रोफाइल को रेट करें और देखें कि क्या आपकी पसंद मेल खाती है।",
            [AppLanguage.Portuguese] = "🌟 <b>Alguém está esperando por você!</b>\n\nAvalie novos perfis e descubra se vocês têm afinidade mútua.",
            [AppLanguage.Indonesian] = "🌟 <b>Ada yang sedang menunggumu!</b>\n\nBeri nilai pada profil baru dan cari tahu apakah kalian saling cocok."
        },
        ["Notification_Inactivity_9"] = new()
        {
            [AppLanguage.Russian] = "🚀 <b>Свежие анкеты уже в поиске!</b>\n\nПосмотри, кто недавно присоединился к DatingBot в твоем регионе.",
            [AppLanguage.Ukrainian] = "🚀 <b>Свіжі анкети вже в пошуку!</b>\n\nПодивись, хто нещодавно приєднався до DatingBot у твоєму регіоні.",
            [AppLanguage.English] = "🚀 <b>Fresh profiles are waiting in search!</b>\n\nCheck out who has recently joined DatingBot in your area.",
            [AppLanguage.Hindi] = "🚀 <b>नए प्रोफाइल खोज में उपलब्ध हैं!</b>\n\nदेखें कि आपके क्षेत्र में हाल ही में DatingBot से कौन जुड़ा है।",
            [AppLanguage.Portuguese] = "🚀 <b>Novos perfis já estão disponíveis na busca!</b>\n\nVeja quem se juntou recentemente ao DatingBot na sua região.",
            [AppLanguage.Indonesian] = "🚀 <b>Profil baru sudah tersedia di pencarian!</b>\n\nLihat siapa saja yang baru bergabung dengan DatingBot di wilayahmu."
        },
        ["Notification_Inactivity_10"] = new()
        {
            [AppLanguage.Russian] = "💖 <b>Любовь не ждет!</b>\n\nЗагляни в бот и найди человека, с которым захочется пойти на свидание.",
            [AppLanguage.Ukrainian] = "💖 <b>Кохання не чекає!</b>\n\nЗавітай у бот і знайди людину, з якою захочеться піти на побачення.",
            [AppLanguage.English] = "💖 <b>Love doesn't wait!</b>\n\nCheck out the bot and find someone you'd love to go on a date with.",
            [AppLanguage.Hindi] = "💖 <b>प्यार इंतज़ार नहीं करता!</b>\n\nबॉट पर आएं और किसी ऐसे व्यक्ति को खोजें जिसके साथ आप डेट पर जाना चाहें।",
            [AppLanguage.Portuguese] = "💖 <b>O amor não espera!</b>\n\nDê uma passada no bot e encontre alguém especial para sair em um encontro.",
            [AppLanguage.Indonesian] = "💖 <b>Cinta tak menunggu!</b>\n\nBuka bot dan temukan seseorang yang ingin kamu ajak berkencan."
        },
        ["SearchDistance_Prompt"] = new()
        {
            [AppLanguage.Russian] = "📍 <b>Выберите дальность поиска анкет</b>\n\n<i>(вы всегда можете изменить все параметры в настройках):</i>",
            [AppLanguage.Ukrainian] = "📍 <b>Оберіть дальність пошуку анкет</b>\n\n<i>(ви завжди можете змінити всі параметри в налаштуваннях):</i>",
            [AppLanguage.English] = "📍 <b>Select search distance for profiles</b>\n\n<i>(you can always change all settings in settings):</i>",
            [AppLanguage.Hindi] = "📍 <b>प्रोफाइल के लिए खोज दूरी चुनें</b>\n\n<i>(आप हमेशा सेटिंग्स में सभी पैरामीटर बदल सकते हैं):</i>",
            [AppLanguage.Portuguese] = "📍 <b>Selecione a distância de busca dos perfis</b>\n\n<i>(você sempre pode alterar todas as preferências nas configurações):</i>",
            [AppLanguage.Indonesian] = "📍 <b>Pilih jarak pencarian profil</b>\n\n<i>(Anda selalu dapat mengubah semua pengaturan di menu pengaturan):</i>"
        },
        ["Distance_UpTo100Km"] = new()
        {
            [AppLanguage.Russian] = "до 100 км",
            [AppLanguage.Ukrainian] = "до 100 км",
            [AppLanguage.English] = "up to 100 km",
            [AppLanguage.Hindi] = "100 किमी तक",
            [AppLanguage.Portuguese] = "até 100 km",
            [AppLanguage.Indonesian] = "hingga 100 km"
        },
        ["Distance_UpTo500Km"] = new()
        {
            [AppLanguage.Russian] = "до 500 км",
            [AppLanguage.Ukrainian] = "до 500 км",
            [AppLanguage.English] = "up to 500 km",
            [AppLanguage.Hindi] = "500 किमी तक",
            [AppLanguage.Portuguese] = "até 500 km",
            [AppLanguage.Indonesian] = "hingga 500 km"
        },
        ["Distance_SameCountry"] = new()
        {
            [AppLanguage.Russian] = "в пределах страны",
            [AppLanguage.Ukrainian] = "у межах країни",
            [AppLanguage.English] = "within country",
            [AppLanguage.Hindi] = "देश के भीतर",
            [AppLanguage.Portuguese] = "no mesmo país",
            [AppLanguage.Indonesian] = "di dalam negeri"
        },
        ["Distance_Anywhere"] = new()
        {
            [AppLanguage.Russian] = "без ограничений",
            [AppLanguage.Ukrainian] = "без обмежень",
            [AppLanguage.English] = "no distance limit",
            [AppLanguage.Hindi] = "बिना किसी सीमा के",
            [AppLanguage.Portuguese] = "sem limites",
            [AppLanguage.Indonesian] = "tanpa batas"
        },
        ["Btn_SearchDistance"] = new()
        {
            [AppLanguage.Russian] = "📍 Дальность поиска",
            [AppLanguage.Ukrainian] = "📍 Дальність пошуку",
            [AppLanguage.English] = "📍 Search distance",
            [AppLanguage.Hindi] = "📍 खोज दूरी",
            [AppLanguage.Portuguese] = "📍 Distância de busca",
            [AppLanguage.Indonesian] = "📍 Jarak pencarian"
        },
        ["Label_SearchDistance"] = new()
        {
            [AppLanguage.Russian] = "Дальность поиска",
            [AppLanguage.Ukrainian] = "Дальність пошуку",
            [AppLanguage.English] = "Search distance",
            [AppLanguage.Hindi] = "खोज दूरी",
            [AppLanguage.Portuguese] = "Distância de busca",
            [AppLanguage.Indonesian] = "Jarak pencarian"
        },
        ["Search_Tip_1"] = new()
        {
            [AppLanguage.Russian] = "💡 Если оценка 6+, человек сможет написать вам в лс",
            [AppLanguage.Ukrainian] = "💡 Якщо оцінка 6+, людина зможе написати вам в ос",
            [AppLanguage.English] = "💡 If rated 6+, the person will be able to send you a direct message",
            [AppLanguage.Hindi] = "💡 यदि रेटिंग 6+ है, तो वह व्यक्ति आपको सीधे संदेश भेज सकेगा",
            [AppLanguage.Portuguese] = "💡 Se a nota for 6+, a pessoa poderá enviar uma mensagem direta para você",
            [AppLanguage.Indonesian] = "💡 Jika nilai 6+, orang tersebut dapat mengirim pesan langsung ke Anda"
        },
        ["Search_Tip_2"] = new()
        {
            [AppLanguage.Russian] = "📢 Официальный канал: @TheBestDating",
            [AppLanguage.Ukrainian] = "📢 Офіційний канал: @TheBestDating",
            [AppLanguage.English] = "📢 Official channel: @TheBestDating",
            [AppLanguage.Hindi] = "📢 आधिकारिक चैनल: @TheBestDating",
            [AppLanguage.Portuguese] = "📢 Canal oficial: @TheBestDating",
            [AppLanguage.Indonesian] = "📢 Saluran resmi: @TheBestDating"
        },
        ["Search_Tip_3"] = new()
        {
            [AppLanguage.Russian] = "💡 Если кто-то что-то продает, это скорей всего мошенники! — жмите \"Пожаловаться\"",
            [AppLanguage.Ukrainian] = "💡 Якщо хтось щось продає, це найімовірніше шахраї! — натискайте \"Поскаржитися\"",
            [AppLanguage.English] = "💡 If someone is selling something, they are likely scammers! — click \"Report\"",
            [AppLanguage.Hindi] = "💡 अगर कोई कुछ बेच रहा है, तो वे धोखेबाज़ हो सकते हैं! — \"शिकायत करें\" पर क्लिक करें",
            [AppLanguage.Portuguese] = "💡 Se alguém estiver vendendo algo, provavelmente é golpe! — clique em \"Denunciar\"",
            [AppLanguage.Indonesian] = "💡 Jika seseorang menjual sesuatu, kemungkinan besar itu penipuan! — klik \"Laporkan\""
        },
        ["Search_Tip_4"] = new()
        {
            [AppLanguage.Russian] = "💡 Мы не пишем пользователям первыми. Если кто-то представляется нашей поддержкой — не отвечайте и жмите Жалоба.",
            [AppLanguage.Ukrainian] = "💡 Ми не пишемо користувачам першими. Якщо хтось представляється нашою підтримкою — не відповідайте та тисніть Скарга.",
            [AppLanguage.English] = "💡 We never message users first. If someone claims to be our support — do not reply and hit Report.",
            [AppLanguage.Hindi] = "💡 हम उपयोगकर्ताओं को पहले कभी संदेश नहीं भेजते। यदि कोई हमारी सहायता टीम होने का दावा करता है — तो जवाब न दें और रिपोर्ट करें।",
            [AppLanguage.Portuguese] = "💡 Nunca enviamos mensagens primeiro. Se alguém fingir ser nosso suporte — não responda e clique em Denunciar.",
            [AppLanguage.Indonesian] = "💡 Kami tidak pernah mengirim pesan lebih dulu. Jika ada yang mengaku dari tim dukungan kami — jangan balas dan klik Laporkan."
        },
        ["Search_Tip_5"] = new()
        {
            [AppLanguage.Russian] = "💡 Если оценка ниже 6, человеку не придет уведомление",
            [AppLanguage.Ukrainian] = "💡 Якщо оцінка нижче 6, людині не надійде сповіщення",
            [AppLanguage.English] = "💡 If the rating is below 6, the person will not receive a notification",
            [AppLanguage.Hindi] = "💡 यदि रेटिंग 6 से कम है, तो व्यक्ति को सूचना नहीं मिलेगी",
            [AppLanguage.Portuguese] = "💡 Se a nota for menor que 6, a pessoa não receberá notificação",
            [AppLanguage.Indonesian] = "💡 Jika nilai di bawah 6, orang tersebut tidak akan menerima notifikasi"
        },
        ["Search_Tip_6"] = new()
        {
            [AppLanguage.Russian] = "💡 Поддержка никогда не пишет первой. Официальная поддержка: @KimeLowe65",
            [AppLanguage.Ukrainian] = "💡 Підтримка ніколи не пише першою. Офіційна підтримка: @KimeLowe65",
            [AppLanguage.English] = "💡 Support never messages first. Official support: @KimeLowe65",
            [AppLanguage.Hindi] = "💡 सपोर्ट टीम कभी भी पहले संदेश नहीं भेजती। आधिकारिक सपोर्ट: @KimeLowe65",
            [AppLanguage.Portuguese] = "💡 O suporte nunca envia mensagens primeiro. Suporte oficial: @KimeLowe65",
            [AppLanguage.Indonesian] = "💡 Dukungan tidak pernah mengirim pesan lebih dulu. Dukungan resmi: @KimeLowe65"
        },
        ["Search_Tip_7"] = new()
        {
            [AppLanguage.Russian] = "💡 Вы можете изменить \"Фильтры\" поиска кандидатов в разделе \"Мой профиль\"",
            [AppLanguage.Ukrainian] = "💡 Ви можете змінити \"Фільтри\" пошуку кандидатів у розділі \"Мій профіль\"",
            [AppLanguage.English] = "💡 You can customize search \"Filters\" in the \"My Profile\" section",
            [AppLanguage.Hindi] = "💡 आप \"मेरी प्रोफ़ाइल\" अनुभाग में उम्मीदवार खोज \"फ़िल्टर\" बदल सकते हैं",
            [AppLanguage.Portuguese] = "💡 Você pode alterar os \"Filtros\" de busca na seção \"Meu Perfil\"",
            [AppLanguage.Indonesian] = "💡 Anda dapat mengubah \"Filter\" pencarian di bagian \"Profil Saya\""
        },
        ["Search_Tip_8"] = new()
        {
            [AppLanguage.Russian] = "💡 Умный поиск бота учитывает ваше AI-описание для точного подбора близких по духу людей.",
            [AppLanguage.Ukrainian] = "💡 Розумний пошук бота враховує ваш AI-опис для точного підбору близьких за духом людей.",
            [AppLanguage.English] = "💡 Smart AI matching analyzes your AI bio to recommend like-minded people.",
            [AppLanguage.Hindi] = "💡 स्मार्ट AI मैचिंग समान विचारधारा वाले लोगों को खोजने के लिए आपके AI विवरण का विश्लेषण करता है।",
            [AppLanguage.Portuguese] = "💡 A busca inteligente analisa sua descrição por IA para encontrar pessoas parecidas com você.",
            [AppLanguage.Indonesian] = "💡 Pencocokan cerdas menganalisis deskripsi AI Anda untuk menemukan orang yang sefrekuensi."
        },
        ["Search_Tip_9"] = new()
        {
            [AppLanguage.Russian] = "💡 Добавьте приветствие в «Мой профиль» — оно выделит вашу анкету среди других в ленте поиска.",
            [AppLanguage.Ukrainian] = "💡 Додайте привітання в «Мій профіль» — воно виділить вашу анкету серед інших у стрічці пошуку.",
            [AppLanguage.English] = "💡 Add a greeting in \"My Profile\" — it will make your profile stand out in the search feed.",
            [AppLanguage.Hindi] = "💡 \"मेरी प्रोफ़ाइल\" में एक अभिवादन जोड़ें — यह फ़ीड में आपकी प्रोफ़ाइल को अलग पहचान देगा।",
            [AppLanguage.Portuguese] = "💡 Adicione uma saudação em \"Meu Perfil\" — isso destacará seu perfil no feed de busca.",
            [AppLanguage.Indonesian] = "💡 Tambahkan salam di \"Profil Saya\" — ini akan membuat profil Anda menonjol di beranda pencarian."
        },
        ["Search_Tip_10"] = new()
        {
            [AppLanguage.Russian] = "💡 В «Фильтрах» можно настроить дальность поиска: до 100 км, 500 км, вся страна или без ограничений.",
            [AppLanguage.Ukrainian] = "💡 У «Фільтрах» можна налаштувати дальність пошуку: до 100 км, 500 км, уся країна або без обмежень.",
            [AppLanguage.English] = "💡 In \"Filters\" you can adjust search distance: up to 100 km, 500 km, whole country, or unlimited.",
            [AppLanguage.Hindi] = "💡 \"फ़िल्टर\" में आप खोज दूरी चुन सकते हैं: 100 किमी, 500 किमी, पूरा देश या असीमित।",
            [AppLanguage.Portuguese] = "💡 Em \"Filtros\" você pode definir a distância de busca: até 100 km, 500 km, todo o país ou ilimitada.",
            [AppLanguage.Indonesian] = "💡 Di \"Filter\" Anda dapat mengatur jarak pencarian: hingga 100 km, 500 km, seluruh negara, atau tanpa batas."
        },
        ["Search_Tip_11"] = new()
        {
            [AppLanguage.Russian] = "💡 Общие интересы выделяются в карточке и помогают быстрее найти темы для первого разговора.",
            [AppLanguage.Ukrainian] = "💡 Спільні інтереси виділяються в картці та допомагають швидше знайти теми для першої розмови.",
            [AppLanguage.English] = "💡 Common interests are highlighted in cards to help you easily start your first conversation.",
            [AppLanguage.Hindi] = "💡 कार्ड में साझा रुचियां हाइलाइट की जाती हैं ताकि बातचीत शुरू करने में आसानी हो।",
            [AppLanguage.Portuguese] = "💡 Interesses em comum são destacados no cartão para facilitar o início da conversa.",
            [AppLanguage.Indonesian] = "💡 Minat bersama disorot pada profil untuk membantu memulai percakapan pertama."
        },
        ["Search_Tip_12"] = new()
        {
            [AppLanguage.Russian] = "💡 Если анкеты закончились, бот автоматически обновит ленту, как только появятся новые кандидаты.",
            [AppLanguage.Ukrainian] = "💡 Якщо анкети закінчилися, бот автоматично оновить стрічку, щойно з'являться нові кандидати.",
            [AppLanguage.English] = "💡 If you run out of profiles, the bot will automatically refresh the feed as new candidates join.",
            [AppLanguage.Hindi] = "💡 यदि प्रोफ़ाइल समाप्त हो जाती हैं, तो नए उम्मीदवार जुड़ने पर बॉट फ़ीड को ताज़ा कर देगा।",
            [AppLanguage.Portuguese] = "💡 Se os perfis acabarem, o bot atualizará o feed automaticamente quando surgirem novos candidatos.",
            [AppLanguage.Indonesian] = "💡 Jika profil habis, bot akan memperbarui feed secara otomatis saat ada kandidat baru."
        },
        ["Search_Tip_13"] = new()
        {
            [AppLanguage.Russian] = "💡 Ищете общение, любовь или флирт? Измените цель знакомства в «Мой профиль» в любой момент.",
            [AppLanguage.Ukrainian] = "💡 Шукаєте спілкування, кохання чи флірт? Змініть мету знайомства в «Мій профіль» у будь-який момент.",
            [AppLanguage.English] = "💡 Looking for friendship, love, or flirt? Change your dating goal in \"My Profile\" anytime.",
            [AppLanguage.Hindi] = "💡 दोस्ती, प्यार या फ़्लर्ट की तलाश है? \"मेरी प्रोफ़ाइल\" में किसी भी समय अपना लक्ष्य बदलें।",
            [AppLanguage.Portuguese] = "💡 Procurando amizade, amor ou paquera? Mude seu objetivo em \"Meu Perfil\" a qualquer momento.",
            [AppLanguage.Indonesian] = "💡 Mencari teman, cinta, atau kencan santai? Ubah tujuan kencan di \"Profil Saya\" kapan saja."
        },
        ["Search_Tip_14"] = new()
        {
            [AppLanguage.Russian] = "💡 Соблюдайте правила сообщества: фейки, оскорбления в анкете и запрещенный контент ведут к блокировке.",
            [AppLanguage.Ukrainian] = "💡 Дотримуйтесь правил спільноти: фейки, образи в анкеті та заборонений контент ведуть до блокування.",
            [AppLanguage.English] = "💡 Follow community guidelines: fakes, insults in profiles, and illicit content lead to a ban.",
            [AppLanguage.Hindi] = "💡 समुदाय के नियमों का पालन करें: फ़ेक प्रोफ़ाइल, अपमान और प्रतिबंधित सामग्री से बैन लग सकता है।",
            [AppLanguage.Portuguese] = "💡 Respeite as regras: perfis falsos, ofensas e conteúdo proibido levam ao banimento.",
            [AppLanguage.Indonesian] = "💡 Patuhi aturan komunitas: profil palsu, hinaan, dan konten terlarang akan diblokir."
        },
        ["Search_Tip_15"] = new()
        {
            [AppLanguage.Russian] = "💡 Анкеты с четким и открытым фото лица получают в несколько раз больше высоких оценок.",
            [AppLanguage.Ukrainian] = "💡 Анкети з чітким і відкритим фото обличчя отримують у кілька разів більше високих оцінок.",
            [AppLanguage.English] = "💡 Profiles with clear, friendly face photos receive significantly more high ratings.",
            [AppLanguage.Hindi] = "💡 स्पष्ट और अच्छे चेहरे वाले फ़ोटो वाली प्रोफ़ाइल को कहीं अधिक उच्च रेटिंग मिलती है।",
            [AppLanguage.Portuguese] = "💡 Perfis com fotos de rosto nítidas e amigáveis recebem muito mais avaliações altas.",
            [AppLanguage.Indonesian] = "💡 Profil dengan foto wajah yang jelas dan ramah mendapatkan lebih banyak nilai tinggi."
        }
    };

    private static readonly Dictionary<string, Dictionary<AppLanguage, string>> InterestsTranslations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["music"] = new()
        {
            [AppLanguage.Russian] = "Музыка",
            [AppLanguage.Ukrainian] = "Музика",
            [AppLanguage.English] = "Music",
            [AppLanguage.Hindi] = "संगीत",
            [AppLanguage.Portuguese] = "Música",
            [AppLanguage.Indonesian] = "Musik"
        },
        ["movies"] = new()
        {
            [AppLanguage.Russian] = "Кино",
            [AppLanguage.Ukrainian] = "Кіно",
            [AppLanguage.English] = "Movies",
            [AppLanguage.Hindi] = "फिल्में",
            [AppLanguage.Portuguese] = "Filmes",
            [AppLanguage.Indonesian] = "Film"
        },
        ["sport"] = new()
        {
            [AppLanguage.Russian] = "Спорт",
            [AppLanguage.Ukrainian] = "Спорт",
            [AppLanguage.English] = "Sports",
            [AppLanguage.Hindi] = "खेल",
            [AppLanguage.Portuguese] = "Esportes",
            [AppLanguage.Indonesian] = "Olahraga"
        },
        ["travel"] = new()
        {
            [AppLanguage.Russian] = "Путешествия",
            [AppLanguage.Ukrainian] = "Подорожі",
            [AppLanguage.English] = "Travel",
            [AppLanguage.Hindi] = "यात्रा",
            [AppLanguage.Portuguese] = "Viagens",
            [AppLanguage.Indonesian] = "Travel"
        },
        ["gaming"] = new()
        {
            [AppLanguage.Russian] = "Видеоигры",
            [AppLanguage.Ukrainian] = "Відеоігри",
            [AppLanguage.English] = "Gaming",
            [AppLanguage.Hindi] = "गेमिंग",
            [AppLanguage.Portuguese] = "Games",
            [AppLanguage.Indonesian] = "Game"
        },
        ["reading"] = new()
        {
            [AppLanguage.Russian] = "Книги",
            [AppLanguage.Ukrainian] = "Книги",
            [AppLanguage.English] = "Books & Reading",
            [AppLanguage.Hindi] = "किताबें",
            [AppLanguage.Portuguese] = "Leitura",
            [AppLanguage.Indonesian] = "Membaca"
        },
        ["art"] = new()
        {
            [AppLanguage.Russian] = "Творчество",
            [AppLanguage.Ukrainian] = "Творчість",
            [AppLanguage.English] = "Art & Creativity",
            [AppLanguage.Hindi] = "कला",
            [AppLanguage.Portuguese] = "Arte",
            [AppLanguage.Indonesian] = "Seni"
        },
        ["cooking"] = new()
        {
            [AppLanguage.Russian] = "Кулинария",
            [AppLanguage.Ukrainian] = "Кулінарія",
            [AppLanguage.English] = "Cooking",
            [AppLanguage.Hindi] = "खाना बनाना",
            [AppLanguage.Portuguese] = "Culinária",
            [AppLanguage.Indonesian] = "Memasak"
        },
        ["tech"] = new()
        {
            [AppLanguage.Russian] = "IT и технологии",
            [AppLanguage.Ukrainian] = "IT та технології",
            [AppLanguage.English] = "Tech & IT",
            [AppLanguage.Hindi] = "तकनीक",
            [AppLanguage.Portuguese] = "Tecnologia",
            [AppLanguage.Indonesian] = "Teknologi"
        },
        ["boardgames"] = new()
        {
            [AppLanguage.Russian] = "Настолки",
            [AppLanguage.Ukrainian] = "Настілки",
            [AppLanguage.English] = "Board games",
            [AppLanguage.Hindi] = "बोर्ड गेम्स",
            [AppLanguage.Portuguese] = "Jogos de tabuleiro",
            [AppLanguage.Indonesian] = "Board game"
        },
        ["outdoor"] = new()
        {
            [AppLanguage.Russian] = "Прогулки",
            [AppLanguage.Ukrainian] = "Прогулянки",
            [AppLanguage.English] = "Outdoors & Walks",
            [AppLanguage.Hindi] = "घूमना-फिरना",
            [AppLanguage.Portuguese] = "Passeios",
            [AppLanguage.Indonesian] = "Jalan-jalan"
        }
    };

    public string Get(AppLanguage language, string key, params object[] args)
    {
        if (Strings.TryGetValue(key, out var langDict))
        {
            if (langDict.TryGetValue(language, out var text) || langDict.TryGetValue(AppLanguage.Russian, out text))
            {
                return args.Length > 0 ? string.Format(text, args) : text;
            }
        }

        return key;
    }

    public string GetGenderText(AppLanguage language, Gender? gender)
    {
        if (!gender.HasValue) return "-";
        return gender == Gender.Male ? Get(language, "Gender_Male") : Get(language, "Gender_Female");
    }

    public string GetTargetGenderText(AppLanguage language, TargetGender? targetGender)
    {
        if (!targetGender.HasValue) return "-";
        return targetGender switch
        {
            TargetGender.Male => Get(language, "TargetGender_Male"),
            TargetGender.Female => Get(language, "TargetGender_Female"),
            _ => Get(language, "TargetGender_All")
        };
    }

    public string GetDatingTargetText(AppLanguage language, DatingTarget? target)
    {
        if (!target.HasValue) return "-";
        return target switch
        {
            DatingTarget.Friends => Get(language, "Target_Friends"),
            DatingTarget.Relationship => Get(language, "Target_Relationship"),
            DatingTarget.AdultOnly => Get(language, "Target_AdultOnly"),
            _ => "-"
        };
    }

    public string GetInterestTitle(AppLanguage language, string key, string fallbackTitle)
    {
        if (InterestsTranslations.TryGetValue(key, out var dict))
        {
            if (dict.TryGetValue(language, out var title) || dict.TryGetValue(AppLanguage.Russian, out title))
            {
                return title;
            }
        }

        return fallbackTitle;
    }

    public string GetMatchBadge(AppLanguage language, string badgeKey, params object[] args)
    {
        return Get(language, badgeKey, args);
    }

    public string FormatCommonInterestsBadge(AppLanguage language, int count)
    {
        var lastTwo = count % 100;
        var lastDigit = count % 10;

        return language switch
        {
            AppLanguage.Ukrainian => (lastTwo is >= 11 and <= 19)
                ? $"🎯 <i>У вас {count} спільних інтересів</i>"
                : (lastDigit == 1)
                    ? $"🎯 <i>У вас {count} спільний інтерес</i>"
                    : (lastDigit is >= 2 and <= 4)
                        ? $"🎯 <i>У вас {count} спільні інтереси</i>"
                        : $"🎯 <i>У вас {count} спільних інтересів</i>",

            AppLanguage.English => (count == 1)
                ? $"🎯 <i>You have {count} common interest</i>"
                : $"🎯 <i>You have {count} common interests</i>",

            AppLanguage.Portuguese => (count == 1)
                ? $"🎯 <i>Vocês têm {count} interesse em comum</i>"
                : $"🎯 <i>Vocês têm {count} interesses em comum</i>",

            AppLanguage.Hindi => $"🎯 <i>आपकी {count} समान रुचियां हैं</i>",

            AppLanguage.Indonesian => $"🎯 <i>Anda memiliki {count} minat yang sama</i>",

            _ => (lastTwo is >= 11 and <= 19)
                ? $"🎯 <i>У вас {count} общих интересов</i>"
                : (lastDigit == 1)
                    ? $"🎯 <i>У вас {count} общий интерес</i>"
                    : (lastDigit is >= 2 and <= 4)
                        ? $"🎯 <i>У вас {count} общих интереса</i>"
                        : $"🎯 <i>У вас {count} общих интересов</i>"
        };
    }

    private static readonly string[] SearchTipKeys =
    [
        "Search_Tip_1",
        "Search_Tip_2",
        "Search_Tip_3",
        "Search_Tip_4",
        "Search_Tip_5",
        "Search_Tip_6",
        "Search_Tip_7",
        "Search_Tip_8",
        "Search_Tip_9",
        "Search_Tip_10",
        "Search_Tip_11",
        "Search_Tip_12",
        "Search_Tip_13",
        "Search_Tip_14",
        "Search_Tip_15"
    ];

    public string GetRandomSearchTip(AppLanguage language)
    {
        var key = SearchTipKeys[Random.Shared.Next(SearchTipKeys.Length)];
        return Get(language, key);
    }

    public IReadOnlyList<string> GetAllSearchTipKeys() => SearchTipKeys;
}