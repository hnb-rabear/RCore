using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RCore
{
	public static class NameGenerator
	{
		public static string GenerateDisplayName(string countryCode)
		{
			if (Random.value < 0.11f)
				return GenerateLatinDisplayName();
			string characterSet = GetCharacterSet(countryCode);
			switch (characterSet)
			{
				case "Cyrillic": return GenerateCyrillicDisplayName();
				case "Chinese": return GenerateChineseDisplayName();
				case "Japanese": return GenerateJapaneseDisplayName();
				case "Korean": return GenerateKoreanDisplayName();
				case "Arabic": return GenerateArabicDisplayName();
				case "Vietnamese": return GenerateVietnameseDisplayName();
				case "Thai": return GenerateThailandDisplayName();
				case "English": return GenerateEnglishDisplayName();
				default: return GenerateLatinDisplayName(); // Default to Latin
			}
		}
		public static string GetCharacterSet(string countryCode)
		{
			if (string.IsNullOrEmpty(countryCode))
				return "English";
			switch (countryCode.ToUpper())
			{
				case "RU":
				case "BY":
				case "BG":
				case "UA":
				case "RS":
				case "MK":
				case "KZ":
				case "KG":
				case "MN":
					return "Cyrillic";
				case "CN":
				case "HK":
				case "TW":
				case "SG":
					return "Chinese";
				case "JP":
					return "Japanese";
				case "KR":
					return "Korean";
				case "SA":
				case "AE":
				case "EG":
				case "IQ":
				case "SY":
				case "JO":
				case "LB":
				case "OM":
				case "QA":
				case "KW":
				case "DZ":
				case "LY":
					return "Arabic";
				case "VN":
					return "Vietnamese";
				case "TH":
					return "Thai";
				case "US":
				case "GB":
				case "CA":
				case "AU":
				case "NZ":
				case "IE":
				case "PH":
				case "ZA":
				case "NG":
				case "IN":
					return "English";
				default:
					return "Latin";
			}
		}
		private static string GenerateCyrillicDisplayName()
		{
			string[] firstPart =
			{
				"Алекс", "Влади", "Конста", "Михай", "Серге", "Дими", "Анато", "Евге", "Никола", "Ива",
				"Оле", "Паве", "Фёдо", "Яро", "Рост", "Григо", "Андре", "Бори", "Леони", "Дани",
				"Тимо", "Арка", "Игна", "Васи", "Кири", "Генна", "Степа", "Матве", "Плато", "Свя",
				"Русла", "Тара", "Фило", "Вене", "Зах", "Саве", "Арсе", "Ереме", "Трофи", "Яков",
				"Эдуа", "Вита", "Эми", "Роди", "Елисе", "Авер", "Агата", "Кла", "Оре", "Про",
				"Мари", "Рема", "Дороф", "Ники", "Ефи", "Гаври", "Лазар", "Фадде", "Миро", "Ови",
				"Кузь", "Добри", "Дени", "Лука", "Мал", "Виль", "Ефре", "Капи", "Стан", "Алекси",
				"Симе", "Владими", "Сама", "Ани", "Вени", "Ил", "Нат", "Станис", "Ярос", "Глеб",
				"Юли", "Юр", "Макси", "Лавре", "Фило", "Рос", "Реми", "Тихо", "Усти", "Пара",
				"Оле", "Осе", "Исаа", "Изи", "Сте", "Каро", "Адр", "Вла", "Оре", "Дав"
			};
			string[] secondPart =
			{
				"ндр", "мир", "нтин", "лов", "евич", "дрей", "ий", "евич", "ич", "рий",
				"гей", "кий", "дор", "слав", "тан", "гор", "ей", "рий", "ий", "лей",
				"тий", "дий", "ций", "фей", "рий", "ей", "тий", "нат", "сей", "нтий",
				"рос", "рий", "тий", "нат", "сим", "фей", "стан", "лев", "мак", "нец",
				"фей", "ский", "ний", "кин", "дор", "мей", "лев", "рин", "рат", "мей",
				"тий", "ван", "сей", "гин", "мит", "якин", "лин", "сь", "вик", "лов",
				"дров", "ник", "пин", "мор", "фин", "ний", "рей", "слин", "дар", "рий",
				"ван", "кий", "гай", "риев", "чен", "чук", "сен", "ндий", "гор", "сий",
				"ров", "лий", "мец", "фей", "рис", "дрик", "кле", "ран", "тин", "рок",
				"то", "мон", "кел", "мей", "дуй", "дот", "чук", "лук", "вец", "кин"
			};
			return firstPart[Random.Range(0, firstPart.Length)] + secondPart[Random.Range(0, secondPart.Length)];
		}
		private static string GenerateChineseDisplayName()
		{
			string[] firstPart =
			{
				"张伟", "王芳", "李强", "刘杰", "陈磊", "杨涛", "赵敏", "孙超", "周军", "吴霞",
				"郑波", "冯涛", "蒋静", "韩磊", "杨丽", "朱敏", "高翔", "黄涛", "何军", "罗丽",
				"胡斌", "梁伟", "宋磊", "谢静", "唐涛", "许杰", "邹波", "崔丽", "程超", "马军",
				"曹敏", "董强", "田丽", "任涛", "钟磊", "谭军", "贺静", "邱超", "余波", "陆杰",
				"夏涛", "蔡敏", "潘磊", "杜静", "黎军", "常涛", "康丽", "龚超", "范军", "孔磊",
				"石敏", "孟涛", "邵杰", "桂丽", "章超", "钱军", "白磊", "江静", "牛涛", "尤军",
				"严磊", "华静", "左涛", "戴军", "龙超", "陶磊", "韦静", "贾涛", "顾军", "毛磊",
				"郝静", "邢涛", "邬军", "安超", "常磊", "段静", "雷涛", "武军", "谭磊", "狄静",
				"滕涛", "温军", "姬磊", "封静", "桑涛", "党军", "宫磊", "路静", "米涛", "乔军",
				"齐磊", "廉静", "车涛", "侯军", "连磊", "漆静", "邰涛", "仲军", "屠磊", "阮静"
			};
			string[] secondPart =
			{
				"俊豪", "晓明", "丽娜", "国鹏", "建华", "雪梅", "志强", "美玲", "海涛", "玉兰",
				"春华", "嘉伟", "秋萍", "晨曦", "志华", "思雨", "志杰", "秀兰", "俊峰", "艳红",
				"小龙", "桂英", "振华", "海霞", "成龙", "丽华", "新宇", "秀珍", "子豪", "海燕",
				"志鹏", "俊英", "少华", "金凤", "浩然", "秀娟", "建国", "静怡", "家豪", "志玲",
				"俊宇", "彩霞", "向东", "玉珍", "浩宇", "晓燕", "光明", "桂芳", "一鸣", "晓莉",
				"志伟", "桂香", "子轩", "淑英", "俊杰", "美珍", "天宇", "红梅", "鸿飞", "春霞",
				"伟杰", "玉华", "志勇", "秀华", "浩翔", "海丽", "子涵", "慧敏", "世杰", "丽红",
				"伟明", "晓辉", "家俊", "婉婷", "国栋", "婷婷", "嘉豪", "玉梅", "昊然", "静茹",
				"志涛", "春燕", "伟宏", "美娜", "文博", "玉凤", "志宁", "月华", "志鑫", "莉莉",
				"文轩", "燕玲", "旭东", "雪芳", "俊楠", "红霞", "鹏飞", "静雯", "世豪", "小霞"
			};
			return firstPart[Random.Range(0, firstPart.Length)] + secondPart[Random.Range(0, secondPart.Length)];
		}
		private static string GenerateJapaneseDisplayName()
		{
			string[] firstPart =
			{
				"田中", "鈴木", "高橋", "山本", "井上", "松本", "佐藤", "中村", "小林", "加藤",
				"吉田", "山田", "渡辺", "藤田", "石井", "原田", "清水", "竹内", "林", "森",
				"池田", "橋本", "山口", "岡田", "坂本", "杉山", "村上", "西村", "大野", "大谷",
				"近藤", "菅原", "内田", "福田", "川上", "安田", "小川", "千葉", "松井", "片山",
				"中川", "柴田", "水野", "木村", "藤井", "長谷川", "坂井", "青木", "浜田", "栗原",
				"三浦", "久保", "石田", "遠藤", "永井", "堀", "平井", "横山", "新井", "市川",
				"丸山", "宮田", "杉本", "中西", "土屋", "金子", "今井", "宇野", "片岡", "河野",
				"大塚", "藤原", "野村", "岡本", "富田", "吉岡", "村田", "高木", "高田", "長田",
				"岩田", "宮本", "高野", "北村", "川村", "島田", "大島", "松田", "古川", "福島",
				"今村", "野口", "川崎", "望月", "田村", "関口", "三宅", "伊藤", "赤坂", "樋口"
			};
			string[] secondPart =
			{
				"一郎", "美咲", "翔太", "優奈", "健吾", "直樹", "香織", "達也", "真由美", "太郎",
				"陽菜", "悠斗", "彩花", "大輔", "千尋", "涼介", "真理", "慎太郎", "玲奈", "悠真",
				"麻衣", "陽翔", "翔子", "颯太", "杏奈", "和也", "由美", "圭佑", "さくら", "拓海",
				"愛理", "陽太", "美穂", "悠人", "友美", "大和", "紗希", "優斗", "恵子", "康太",
				"真紀", "俊介", "美優", "亮介", "陽子", "陽樹", "梨花", "直也", "理奈", "優輝",
				"奈緒", "裕也", "結衣", "悠", "萌", "佑介", "聡子", "剛", "菜々", "誠",
				"彩乃", "雄太", "愛", "真琴", "光", "美咲", "優太", "香奈", "勇輝", "咲",
				"弘樹", "さゆり", "慎", "綾乃", "龍太郎", "千夏", "仁", "玲子", "圭吾", "友香",
				"雅人", "琴音", "篤志", "奈々子", "琢磨", "葉月", "達郎", "悠希", "祐輔", "琴美",
				"義人", "美帆", "大樹", "沙織", "隼人", "莉奈", "拓真", "陽菜", "宗一郎", "陽葵"
			};
			return firstPart[Random.Range(0, firstPart.Length)] + secondPart[Random.Range(0, secondPart.Length)];
		}
		private static string GenerateKoreanDisplayName()
		{
			string[] firstPart =
			{
				"김준", "이현", "박민", "최성", "정우", "강호", "조영", "한결", "윤서", "신태",
				"손혁", "서진", "배수", "송혁", "남건", "임규", "문찬", "권도", "홍기", "안빈",
				"백훈", "심재", "양태", "노경", "조운", "전범", "강윤", "유빈", "장혁", "석호",
				"표진", "황성", "제훈", "명진", "길현", "은찬", "민수", "도현", "기태", "상우",
				"영민", "준혁", "우진", "성훈", "태윤", "진서", "수호", "동현", "정빈", "건우",
				"상진", "재훈", "시후", "석준", "지환", "현우", "희준", "태호", "도영", "규현",
				"현빈", "유찬", "형준", "시온", "동우", "태민", "성준", "우석", "윤호", "규민",
				"찬우", "수민", "정환", "영훈", "승현", "지훈", "도준", "세준", "유현", "석훈",
				"현준", "민혁", "시영", "재민", "건호", "명훈", "진우", "태경", "승우", "혁진",
				"규태", "진혁", "지용", "선우", "태우", "찬혁", "상민", "하늘", "윤재", "재욱"
			};
			string[] secondPart =
			{
				"호", "哲", "英", "美", "泰", "昊", "浩", "然", "宇", "赫",
				"俊", "誠", "熙", "真", "成", "基", "元", "哲", "烈", "濟",
				"炫", "昱", "順", "潤", "圭", "天", "昌", "政", "榮", "佑",
				"泰", "建", "希", "民", "燦", "和", "勇", "恒", "一", "承",
				"厚", "祥", "俊", "澤", "秀", "宰", "善", "弘", "尚", "洙",
				"範", "佑", "鎮", "東", "弼", "誠", "恩", "載", "宣", "俊",
				"玟", "祥", "赫", "錫", "弘", "慶", "權", "洙", "辰", "峰",
				"弘", "彬", "煥", "鍾", "植", "宸", "珉", "翼", "賢", "德",
				"燦", "楨", "基", "善", "弦", "昊", "智", "翰", "潤", "勇",
				"恒", "鎬", "東", "宙", "淳", "洙", "燦", "玟", "誠", "熙"
			};
			return firstPart[Random.Range(0, firstPart.Length)] + secondPart[Random.Range(0, secondPart.Length)];
		}
		private static string GenerateArabicDisplayName()
		{
			string[] firstPart =
			{
				"عبد", "محمد", "أحمد", "يوسف", "سليم", "إبري", "خالد", "علي", "حسن", "راشد",
				"سعد", "عمر", "طارق", "مروان", "بدر", "نواف", "فيصل", "ماجد", "زياد", "فهد",
				"أنس", "لؤي", "أكرم", "أوس", "تامر", "هاشم", "هيثم", "ثامر", "بلال", "سامر",
				"فراس", "خليل", "شادي", "مالك", "يزيد", "براء", "كريم", "محسن", "بشار", "طلال",
				"زياد", "حمزة", "رمزي", "عادل", "وسيم", "معتز", "شريف", "رائد", "داود", "إياد",
				"يحيى", "مراد", "عماد", "مجد", "أيمن", "مهند", "ربيع", "علاء", "ضياء", "حارث",
				"نزار", "جواد", "سراج", "أنور", "عباس", "مكرم", "صفوان", "وائل", "حاتم", "مازن",
				"بهي", "كاظم", "زاهر", "إيهاب", "شاكر", "راغب", "حكمت", "وسام", "نعيم", "إدريس"
			};
			string[] secondPart =
			{
				"الله", "سعد", "حسن", "رحم", "مهدي", "صدي", "كري", "هادي", "شري", "عاب",
				"مبا", "منص", "فتح", "لطيف", "حميد", "عظي", "غفو", "رشيد", "حكي", "وال",
				"معي", "راش", "مجيد", "رؤو", "صبر", "بشي", "ماجد", "متو", "مهيم", "سعي",
				"شاكر", "رزاق", "علي", "نور", "قدير", "ودود", "رفيق", "منت", "عدل", "ولي",
				"شاف", "ظاهر", "باسط", "منير", "متين", "شهيد", "غني", "قدوس", "سمي", "مصور"
			};
			return firstPart[Random.Range(0, firstPart.Length)] + secondPart[Random.Range(0, secondPart.Length)];
		}
		private static string GenerateVietnameseDisplayName()
		{
			string[] firstPart =
			{
				"Nguyen", "Tran", "Le", "Pham", "Hoang", "Bui", "Dang", "Do", "Dinh", "Duong",
				"Huynh", "Luu", "Ly", "Mac", "Ngo", "Ngoc", "Ninh", "Quach", "Thai", "Trinh",
				"Truong", "Tu", "Vu", "An", "Bach", "Cao", "Chau", "Chu", "Cong", "Cuong",
				"Danh", "Diep", "Gia", "Giang", "Ha", "Hanh", "Hien", "Hong", "Huu", "Khai",
				"Khanh", "Khieu", "Kim", "Kinh", "Lai", "Lam", "Lang", "Linh", "Long", "Mai",
				"Minh", "Nam", "Nghia", "Nhan", "Nhat", "Phan", "Phong", "Phu", "Quoc", "Quyen",
				"Sang", "Son", "Sy", "Tai", "Tam", "Tan", "Thach", "Thanh", "Thao", "Thien",
				"Thong", "Thuan", "Tien", "Toan", "Trac", "Tri", "Trieu", "Trong", "Tuong", "Uy",
				"Van", "Vi", "Viet", "Vinh", "Xuan", "Yen", "Hau", "Lien", "Loan", "Nga",
				"Nguyet", "Oanh", "Phuong", "Quynh", "Tham", "Thuy", "Trang", "Truc", "Tuyet", "Vy"
			};
			string[] secondPart =
			{
				"Thanh", "Quang", "Minh", "Anh", "Tuan", "Bao", "Binh", "Cao", "Chinh", "Cong",
				"Cuong", "Dai", "Dang", "Dat", "Dinh", "Duc", "Duy", "Giang", "Ha", "Hao",
				"Hiep", "Hieu", "Hoang", "Hong", "Huu", "Khanh", "Khoa", "Kien", "Lam", "Linh",
				"Long", "Luan", "Luong", "Mai", "Manh", "Nam", "Nghia", "Nhan", "Nhat", "Phong",
				"Phuc", "Phuoc", "Quoc", "Quy", "Sang", "Son", "Sy", "Tai", "Tam", "Tan",
				"Thach", "Thang", "Thao", "Thien", "Thinh", "Thong", "Thu", "Thuan", "Tien", "Toan",
				"Trieu", "Trong", "Tuan", "Tung", "Tuong", "Uy", "Van", "Vi", "Viet", "Vinh",
				"Xuan", "Yen", "An", "Bach", "Chau", "Diep", "Dong", "Hanh", "Hau", "Hoan",
				"Huyen", "Khiem", "Lan", "Lien", "Loan", "Mai", "Nga", "Ngoc", "Nhi", "Nu",
				"Oanh", "Phuong", "Quyen", "Quynh", "Tam", "Tham", "Thuy", "Trang", "Trinh", "Tuyet"
			};
			string first = firstPart[Random.Range(0, firstPart.Length)];
			string second = secondPart[Random.Range(0, secondPart.Length)];
			if (first.Length <= 3 || second.Length <= 3)
				return $"{first} {second}";
			var rad = Random.value;
			return rad switch
			{
				< 0.4f => Random.value > 0.5f ? first : first.ToLower(),
				< 0.8f => Random.value > 0.5f ? second : second.ToLower(),
				_ => Random.value > 0.5f ? $"{first} {second}" : $"{first.ToLower()} {second.ToLower()}"
			};
		}
		private static string GenerateThailandDisplayName()
		{
			string[] firstPart =
			{
				"นัท", "บีม", "ตูน", "ฟ้า", "จูน", "แก้ม", "ดิว", "กาย", "ต้น", "เบียร์",
				"โอ๊ต", "ไนซ์", "มิกซ์", "พล", "ก้อง", "เนย", "โอ", "โต้ง", "เจน", "แนน",
				"แพท", "กุ้ง", "บัว", "แทน", "คิว", "แอม", "จุ๊บ", "เป้", "ท๊อป", "หมิว",
				"พีช", "ต้น", "หมอก", "จิ๋ว", "ปอ", "ตั้ม", "ปิง", "ยิ้ม", "ซัน", "เอิร์ธ",
				"นิค", "เจ", "ส้ม", "จอย", "พลอย", "เมย์", "ฟาง", "ชม", "เบล", "บอส",
				"กาย", "โอ", "แชมป์", "ฟิล์ม", "โอม", "ซาร่า", "โต๋", "บูม", "เอ็ม", "มาร์ค",
				"ไอซ์", "เนส", "อาร์ม", "เอม", "บอม", "มิว", "เป้", "เฟิร์น", "กิ๊ฟ", "ตาล",
				"บุ๊ค", "จิ๊บ", "ฝน", "ปัน", "ปอ", "จีจี้", "แพรว", "ขิม", "แอน", "โอ๋",
				"เกด", "กุ้ง", "พลอย", "พิม", "ฝ้าย", "นุ่น", "แพน", "ข้าว", "จิน", "เมย์",
				"กี้", "นิว", "บิ๊ก", "เติ้ล", "แจ๊ค", "หมอ", "ภพ", "เล็ก", "โตโต้", "ซิน"
			};
			string[] secondPart =
			{
				"นี", "โต", "ชัย", "ดา", "บี", "โอ", "นะ", "แจ", "ติ", "ริ",
				"ฟู", "จู", "ดี้", "พล", "แซม", "แท", "กิ๊", "มิว", "ซู", "โซ",
				"ขวัญ", "หลิน", "ฝัน", "หมวย", "นก", "โบ", "หงส์", "อิง", "ฟ้า", "ขจี",
				"กัน", "แจน", "เดย์", "เต๋อ", "ปิง", "ขนุน", "หนู", "โม", "รัน", "พี",
				"บัว", "พง", "ข้าว", "นา", "ติ๋ว", "นัท", "โอ๊ต", "หนิง", "ทอม", "เฟิร์น",
				"โจ๊ก", "แพน", "ก้อย", "จีจี้", "พร", "กาย", "ส้ม", "ขจร", "บุ๋ม", "โอ๋",
				"นิว", "ต้น", "ฟิล์ม", "ซิน", "เมย์", "เบน", "เติ้ล", "โท", "แจ๊ค", "เพชร",
				"ภู", "แก้ม", "ปุ้ย", "เคน", "เพียว", "แอม", "หนู", "ขิม", "ชมพู่", "มิ้น",
				"โบว์", "แพรว", "จีจี้", "ฝ้าย", "แอน", "ดิว", "มิว", "อั้ม", "บุ๊ค", "บิว",
				"บิ๊ก", "โม", "ตาล", "บาส", "ตั้ม", "หมอ", "ภพ", "เมฆ", "เล็ก", "ซัน"
			};
			return firstPart[Random.Range(0, firstPart.Length)] + secondPart[Random.Range(0, secondPart.Length)];
		}
		private static string GenerateLatinDisplayName()
		{
			string[] firstPart =
			{
				"Luca", "Gino", "Brio", "Hugo", "Nico", "Paco", "Tino", "Rico", "Leo", "Tito",
				"Milo", "Dino", "Vito", "Lino", "Enzo", "Ciro", "Filo", "Remo", "Elio", "Simo",
				"Javi", "Beni", "Davi", "Ludo", "Reno", "Caro", "Gabe", "Juli", "Talo", "Ramo",
				"Manu", "Jano", "Miro", "Pipo", "Teo", "Ugo", "Tano", "Otto", "Raul", "Emil",
				"Alba", "Cleo", "Lila", "Mila", "Rita", "Tara", "Vera", "Zara", "Luna", "Nina",
				"Rosa", "Ines", "Aida", "Luci", "Gala", "Maia", "Noel", "Cira", "Leta", "Vina",
				"Juno", "Zeno", "Orla", "Nilo", "Vina", "Arlo", "Iker", "Omar", "Pere", "Quim",
				"Saul", "Uri", "Xavi", "Yago", "Zeno", "Biel", "Gus", "Ivo", "Max", "Ned",
				"Oto", "Rex", "Sil", "Tom", "Ugo", "Val", "Wil", "Xan", "Yan", "Zac"
			};
			string[] secondPart =
			{
				"Vas", "Rui", "San", "Leo", "Paz", "Sol", "Mar", "Lux", "Cruz", "Fel",
				"Tiz", "Jul", "Hugo", "Luca", "Dino", "Enzo", "Rico", "Milo", "Otto", "Nico",
				"Raul", "Ugo", "Tito", "Beni", "Ciro", "Reno", "Tano", "Remo", "Gino", "Javi",
				"Lino", "Simo", "Talo", "Manu", "Davi", "Jano", "Miro", "Pipo", "Teo", "Elio",
				"Ludo", "Gabe", "Juli", "Ramo", "Caro", "Biel", "Filo", "Tito", "Zeno", "Hugo",
				"Lino", "Otto", "Reno", "Sil", "Max", "Ned", "Oto", "Rex", "Tom", "Ugo",
				"Val", "Wil", "Xan", "Yan", "Zac", "Ivo", "Gus", "Saul", "Uri", "Xavi",
				"Yago", "Zeno", "Biel", "Gus", "Ivo", "Max", "Ned", "Oto", "Rex", "Sil",
				"Tom", "Ugo", "Val", "Wil", "Xan", "Yan", "Zac", "Iker", "Omar", "Pere",
				"Quim", "Arlo", "Orla", "Nilo", "Vina", "Juno", "Cira", "Leta", "Vina", "Maia"
			};
			string first = firstPart[Random.Range(0, firstPart.Length)];
			string second = secondPart[Random.Range(0, secondPart.Length)];
			if (first.Length <= 3 || second.Length <= 3)
				return $"{first} {second}";
			var rad = Random.value;
			return rad switch
			{
				< 0.4f => Random.value > 0.5f ? first : first.ToLower(),
				< 0.8f => Random.value > 0.5f ? second : second.ToLower(),
				_ => Random.value > 0.5f ? $"{first} {second}" : $"{first.ToLower()} {second.ToLower()}"
			};
		}
		private static string GenerateEnglishDisplayName()
		{
			string[] firstPart =
			{
				"Bob", "Max", "Tim", "Sam", "Joe", "Tom", "Ben", "Ned", "Jay", "Roy",
				"Leo", "Gus", "Zed", "Lou", "Dex", "Ace", "Bud", "Rex", "Hal", "Jax",
				"Moe", "Sid", "Cal", "Hank", "Buck", "Duke", "Skip", "Biff", "Chip", "Zane",
				"Fizz", "Boom", "Zap", "Buzz", "Taz", "Zig", "Fuzz", "Quip", "Wiz", "Jinx",
				"Dash", "Riff", "Vex", "Zork", "Plop", "Twix", "Flip", "Bam", "Bop", "Zip"
			};
			string[] secondPart =
			{
				"Zap", "Fizz", "Boom", "Buzz", "Bop", "Jinx", "Zork", "Wiz", "Twix", "Dash",
				"Zed", "Moo", "Pug", "Duke", "Rex", "Max", "Taz", "Biff", "Plop", "Fuzz",
				"Quip", "Chomp", "Flip", "Dork", "Zig", "Chop", "Vex", "Bonk", "Skip", "Honk",
				"Gulp", "Funk", "Bash", "Munch", "Jolt", "Twist", "Boop", "Snip", "Womp", "Zonk",
				"Riff", "Thud", "Boing", "Splish", "Blip", "Slap", "Tonk", "Twitch", "Splat", "Clap"
			};
			string first = firstPart[Random.Range(0, firstPart.Length)];
			string second = secondPart[Random.Range(0, secondPart.Length)];
			if (first.Length <= 3 || second.Length <= 3)
				return $"{first} {second}";
			var rad = Random.value;
			return rad switch
			{
				< 0.4f => Random.value > 0.5f ? first : first.ToLower(),
				< 0.8f => Random.value > 0.5f ? second : second.ToLower(),
				_ => Random.value > 0.5f ? $"{first} {second}" : $"{first.ToLower()} {second.ToLower()}"
			};
		}
	}
}