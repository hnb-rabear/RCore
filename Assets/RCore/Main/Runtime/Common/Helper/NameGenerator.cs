using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RCore
{
	public static class NameGenerator
	{
#region Name Arrays

		private static readonly string[] CyrillicFirst =
		{
			"Александр", "Дмитрий", "Максим", "Сергей", "Андрей", "Алексей", "Артём", "Илья", "Кирилл", "Михаил",
			"Никита", "Матвей", "Роман", "Егор", "Арсений", "Иван", "Денис", "Евгений", "Даниил", "Тимофей",
			"Владислав", "Игорь", "Владимир", "Павел", "Руслан", "Марк", "Константин", "Тимур", "Олег", "Ярослав",
			"Антон", "Николай", "Глеб", "Давид", "Георгий", "Богдан", "Станислав", "Юрий", "Вадим", "Степан",
			"Анастасия", "Мария", "Анна", "Виктория", "Екатерина", "Ксения", "Алиса", "Вероника", "Полина", "Дарья"
		};
		private static readonly string[] CyrillicSecond =
		{
			"Иванов", "Смирнов", "Кузнецов", "Попов", "Васильев", "Петров", "Соколов", "Михайлов", "Новиков", "Фёдоров",
			"Морозов", "Волков", "Алексеев", "Лебедев", "Семёнов", "Егоров", "Павлов", "Козлов", "Степанов", "Николаев",
			"Орлов", "Андреев", "Макаров", "Никитин", "Захаров", "Зайцев", "Соловьёв", "Борисов", "Яковлев", "Григорьев",
			"Романов", "Воробьёв", "Сергеев", "Кузьмин", "Фролов", "Александров", "Дмитриев", "Королёв", "Гусев", "Киселёв",
			"Ильин", "Максимов", "Поляков", "Сорокин", "Виноградов", "Ковалёв", "Белов", "Медведев", "Антонов", "Тарасов"
		};

		private static readonly string[] ChineseFirst =
		{
			"李", "王", "张", "刘", "陈", "杨", "赵", "黄", "周", "吴",
			"徐", "孙", "hu", "朱", "高", "林", "何", "郭", "马", "罗",
			"梁", "宋", "郑", "谢", "韩", "唐", "冯", "于", "董", "萧",
			"程", "曹", "袁", "邓", "许", "傅", "沈", "曾", "彭", "吕",
			"苏", "卢", "蒋", "蔡", "贾", "丁", "魏", "薛", "叶", "阎",
			"余", "潘", "杜", "戴", "夏", "钟", "汪", "田", "任", "姜"
		};
		private static readonly string[] ChineseSecond =
		{
			"伟", "芳", "娜", "敏", "静", "强", "磊", "军", "洋", "勇",
			"艳", "杰", "娟", "涛", "明", "超", "秀", "霞", "平", "刚",
			"桂", "英", "华", "玲", "红", "玉", "兰", "珍", "莉", "云",
			"飞", "鹏", "强", "浩", "波", "良", "辉", "健", "铭", "凯",
			"文", "博", "思", "诗", "雅", "婷", "涵", "梦", "琪", "悦",
			"宇", "航", "天", "昊", "然", "轩", "辰", "逸", "泽", "阳"
		};

		private static readonly string[] JapaneseFirst =
		{
			"佐藤", "鈴木", "高橋", "田中", "伊藤", "渡辺", "山本", "中村", "小林", "加藤",
			"吉田", "山田", "佐々木", "山口", "松本", "井上", "木村", "林", "斎藤", "清水",
			"山崎", "森", "池田", "橋本", "阿部", "石川", "山下", "中島", "石井", "小川",
			"前田", "岡田", "長谷川", "藤田", "後藤", "近藤", "村上", "遠藤", "青木", "坂本",
			"斉藤", "福田", "太田", "西村", "藤井", "金子", "和田", "中山", "村田", "三浦"
		};
		private static readonly string[] JapaneseSecond =
		{
			"翔太", "拓海", "健太", "大輝", "優斗", "大輔", "和也", "達也", "直人", "哲也",
			"陽 一", "健", "誠", "剛", "修", "陸", "翼", "駿", "勇樹", "大樹",
			"美咲", "愛", "優奈", "陽菜", "美優", "七海", "未来", "花", "葵", "結 衣",
			"彩", "優", "真 由", "香 織", "千 尋", "理 恵", "亜 美", "麻 衣", "恵", "由 美",
			"太郎", "次郎", "三郎", "健太郎", "慎之介", "龍之介", "光", "宏", "博", "隆"
		};

		private static readonly string[] KoreanFirst =
		{
			"김", "이", "박", "최", "정", "강", "조", "윤", "장", "임",
			"한", "오", "서", "신", "권", "황", "안", "송", "류", "전",
			"홍", "고", "문", "양", "손", "배", "조", "백", "허", "유",
			"남", "심", "노", "정", "하", "곽", "성", "차", "주", "우",
			"구", "신", "임", "나", "전", "민", "유", "진", "지", "엄"
		};
		private static readonly string[] KoreanSecond =
		{
			"서준", "민준", "도윤", "예준", "시우", "하준", "주원", "지호", "지후", "준우",
			"준서", "건우", "현우", "지훈", "유준", "우진", "선우", "서진", "연우", "정우",
			"서연", "서윤", "지우", "서현", "하은", "하윤", "민서", "지유", "윤서", "채원",
			"지아", "은서", "다은", "예은", "수아", "소율", "지민", "유진", "서영", "지원",
			"준호", "호진", "도현", "현준", "승우", "승현", "수민", "시윤", "진우", "재원"
		};

		private static readonly string[] ArabicFirst =
		{
			"محمد", "أحمد", "علي", "عمر", "إبراهيم", "يوسف", "حسن", "حسين", "عبدالله", "خالد",
			"فهد", "سلمان", "فيصل", "سعود", "راشد", "سعيد", "عبدالرحمن", "صالح", "ماجد", "نايف",
			"سلطان", "تركي", "بدر", "وليد", "محمود", "مصطفى", "طارق", "كريم", "زياد", "ياسر",
			"أمير", "حمزة", "بلال", "عماد", "مهدي", "مازن", "سامي", "هيثم", "رامي", "باسل",
			"فاطمة", "مريم", "سارة", "نورة", "عائشة", "زينب", "ريم", "ليلى", "أمل", "منى"
		};
		private static readonly string[] ArabicSecond =
		{
			"الغامدي", "القحطاني", "الدوسري", "العتيبي", "الشهري", "الزهراني", "العنزي", "المالكي", "المطيري", "الحربي",
			"الشمراني", "الأسمري", "القرني", "السبيعي", "اليامي", "الخالدي", "باوزير", "المهندس", "النججار", "الحداد",
			"الشيخ", "السيد", "الحسن", "حسين", "علي", "عثمان", "بكر", "عمر", "صالح", "إبراهيم",
			"خليفة", "سليمان", "يونس", "يعقوب", "إسماعيل", "موسى", "عيسى", "داود", "نوح", "ادم",
			"المصري", "الشامي", "العراق", "اليمن", "الكويت", "عمان", "قطر", "بحرين", "سعود", "فيصل"
		};

		private static readonly string[] VietnameseFirst =
		{
			"Nguyen", "Tran", "Le", "Pham", "Hoang", "Huynh", "Phan", "Vu", "Vo", "Dang",
			"Bui", "Do", "Ho", "Ngo", "Duong", "Ly", "An", "Bach", "Banh", "Bao",
			"Cao", "Chau", "Che", "Chu", "Chung", "Diep", "Dinh", "Doan", "Dong", "Du",
			"Ha", "Han", "Hieu", "Hua", "Kieu", "Kim", "La", "Lac", "Lam", "Lieu",
			"Luc", "Luong", "Luu", "Ma", "Mach", "Mai", "Nghiem", "Nham", "Nhu", "Nong"
		};
		private static readonly string[] VietnameseSecond =
		{
			"Anh", "Minh", "Tuan", "Duc", "Duy", "Hoang", "Long", "Nam", "Phuc", "Quan",
			"Son", "Thang", "Thanh", "Thien", "Toan", "Trung", "Viet", "Vinh", "Hai", "Hung",
			"Huy", "Khai", "Khanh", "Khoa", "Kien", "Lam", "Linh", "Luan", "Nhan", "Nhat",
			"Phi", "Phong", "Phu", "Quoc", "Tan", "Thai", "Thinh", "Tien", "Tin", "Tri",
			"Trong", "Truong", "Tung", "Vy", "Yen", "Hoa", "Lan", "Mai", "Ngoc", "Phuong"
		};

		private static readonly string[] ThaiFirst =
		{
			"สมชาย", "สมศักดิ์", "อาทิตย์", "กิตติศักดิ์", "ประเสริฐ", "ธงชัย", "วิชัย", "สมบัติ", "ศักดา", "วินัย",
			"สุรชัย", "ณรงค์", "ปรีชา", "สมพงษ์", "สุวิทย์", "ธวัช", "ปัญญา", "ฉัตรชัย", "วิรัตน์", "อำนาจ",
			"รัตนา", "มาลี", "วรรณนิภา", "สมจิต", "กัญญา", "นิภา", "สุนิสา", "สุภาพร", "อุไรวรรณ", "อัมพร",
			"นนท์", "บีม", "ปอ", "เมย์", "ฟ้า", "กอล์ฟ", "แบงค์", "เฟิร์น", "มิ้นท์", "มายด์",
			"ปาล์ม", "เตย", "นิว", "เอิร์ธ", "ไอซ์", "เบส", "ไบร์ท", "วิน", "กัน", "มาร์ค"
		};
		private static readonly string[] ThaiSecond =
		{
			"แสนดี", "รักไทย", "ใจดี", "มีสุข", "เจริญ", "สวัสดิ์", "วงศ์", "ศรีสุข", "ทอง", "แก้ว",
			"ณ อยุธยา", "รัตนพร", "วงษ์สุวรรณ", "จันทร์โอชา", "ชินวัตร", "เวชชาชีวะ", "หลีกภัย", "ศิลปอาชา", "ชุณหะวัณ", "สุนทรเวช",
			"กิตติขจร", "ปราโมช", "ภิรมย์ภักดี", "จิราธิวัฒน์", "เจียรวนนท์", "สิริวัฒนภักดี", "อยู่วิทยา", "ศรีวัฒนประภา", "รักศรีอักษร", "ล่ำซำ",
			"โสภณพนิช", "โพธิรัตนางกูร", "กาญจนพาสน์", "พรประภา", "โอสถานุเคราะห์", "ว่องกุศลกิจ", "รัตนรักษ์", "เจียรวนนท์", "เหมรัชต", "วิไลลักษณ์"
		};

		private static readonly string[] LatinFirst =
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
		private static readonly string[] LatinSecond =
		{
			"Vas", "Rui", "San", "Leo", "Paz", "Sol", "Mar", "Lux", "Cruz", "Fel",
			"Tiz", "Jul", "Hugo", "Luca", "Dino", "Enzo", "Rico", "Milo", "Otto", "Nico",
			"Raul", "Ugo", "Tito", "Beni", "Ciro", "Reno", "Tano", "Remo", "Gino", "Javi",
			"Lino", "Simo", "Talo", "Manu", "Davi", "Jano", "Miro", "Pipo", "Teo", "Elio",
			"Ludo", "Gabe", "Juli", "Ramo", "Caro", "Biel", "Filo", "Tito", "Zeno", "Hugo",
			"Lino", "Otto", "Reno", "Sil", "Max", "Ned", "Oto", "Rex", "Sil",
			"Tom", "Ugo", "Val", "Wil", "Xan", "Yan", "Zac", "Ivo", "Gus", "Saul", "Uri",
			"Xavi",
			"Yago", "Zeno", "Biel", "Gus", "Ivo", "Max", "Ned", "Oto", "Rex", "Sil",
			"Tom", "Ugo", "Val", "Wil", "Xan", "Yan", "Zac", "Iker", "Omar", "Pere",
			"Quim", "Arlo", "Orla", "Nilo", "Vina", "Juno", "Cira", "Leta", "Vina", "Maia"
		};

		private static readonly string[] EnglishFirst =
		{
			"Bob", "Max", "Tim", "Sam", "Joe", "Tom", "Ben", "Ned", "Jay", "Roy",
			"Leo", "Gus", "Zed", "Lou", "Dex", "Ace", "Bud", "Rex", "Hal", "Jax",
			"Moe", "Sid", "Cal", "Hank", "Buck", "Duke", "Skip", "Biff", "Chip", "Zane",
			"Fizz", "Boom", "Zap", "Buzz", "Taz", "Zig", "Fuzz", "Quip", "Wiz", "Jinx",
			"Dash", "Riff", "Vex", "Zork", "Plop", "Twix", "Flip", "Bam", "Bop", "Zip"
		};
		private static readonly string[] EnglishSecond =
		{
			"Zap", "Fizz", "Boom", "Buzz", "Bop", "Jinx", "Zork", "Wiz", "Twix", "Dash",
			"Zed", "Moo", "Pug", "Duke", "Rex", "Max", "Taz", "Biff", "Plop", "Fuzz",
			"Quip", "Chomp", "Flip", "Dork", "Zig", "Chop", "Vex", "Bonk", "Skip", "Honk",
			"Gulp", "Funk", "Bash", "Munch", "Jolt", "Twist", "Boop", "Snip", "Womp", "Zonk",
			"Riff", "Thud", "Boing", "Splish", "Blip", "Slap", "Tonk", "Twitch", "Splat", "Clap"
		};

#endregion

		public static string GenerateDisplayName(string countryCode)
		{
			if (Random.value < 0.2f)
				return GenerateStupidName();
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

		public static void LogAllCharacters()
		{
			var allArrays = new List<string[]>
			{
				CyrillicFirst, CyrillicSecond,
				ChineseFirst, ChineseSecond,
				JapaneseFirst, JapaneseSecond,
				KoreanFirst, KoreanSecond,
				ArabicFirst, ArabicSecond,
				VietnameseFirst, VietnameseSecond,
				ThaiFirst, ThaiSecond,
				LatinFirst, LatinSecond,
				EnglishFirst, EnglishSecond,
				StupidFirst, StupidSecond
			};

			var set = new HashSet<char>();
			foreach (var array in allArrays)
			{
				foreach (var str in array)
				{
					foreach (var c in str)
					{
						if (!set.Contains(c))
							set.Add(c);
					}
				}
			}

			var chars = new List<char>(set);
			chars.Sort();
			string result = new string(chars.ToArray());
			Debug.Log(result);
		}
		private static string GenerateCyrillicDisplayName()
		{
			// First Name + Space + Last Name
			return CyrillicFirst[Random.Range(0, CyrillicFirst.Length)] + " " + CyrillicSecond[Random.Range(0, CyrillicSecond.Length)];
		}
		private static string GenerateChineseDisplayName()
		{
			// Surname + Given Name (No Space)
			return ChineseFirst[Random.Range(0, ChineseFirst.Length)] + ChineseSecond[Random.Range(0, ChineseSecond.Length)];
		}
		private static string GenerateJapaneseDisplayName()
		{
			// Surname + Given Name (No Space)
			return JapaneseFirst[Random.Range(0, JapaneseFirst.Length)] + JapaneseSecond[Random.Range(0, JapaneseSecond.Length)];
		}
		private static string GenerateKoreanDisplayName()
		{
			// Surname + Given Name (No Space)
			return KoreanFirst[Random.Range(0, KoreanFirst.Length)] + KoreanSecond[Random.Range(0, KoreanSecond.Length)];
		}
		private static string GenerateArabicDisplayName()
		{
			// First Name + Space + Last Name
			return ArabicFirst[Random.Range(0, ArabicFirst.Length)] + " " + ArabicSecond[Random.Range(0, ArabicSecond.Length)];
		}
		private static string GenerateVietnameseDisplayName()
		{
			// Surname + Space + Given Name
			return VietnameseFirst[Random.Range(0, VietnameseFirst.Length)] + " " + VietnameseSecond[Random.Range(0, VietnameseSecond.Length)];
		}
		private static string GenerateThailandDisplayName()
		{
			return ThaiFirst[Random.Range(0, ThaiFirst.Length)] + " " + ThaiSecond[Random.Range(0, ThaiSecond.Length)];
		}
		private static string GenerateLatinDisplayName()
		{
			return LatinFirst[Random.Range(0, LatinFirst.Length)] + " " + LatinSecond[Random.Range(0, LatinSecond.Length)];
		}
		private static string GenerateEnglishDisplayName()
		{
			return EnglishFirst[Random.Range(0, EnglishFirst.Length)] + " " + EnglishSecond[Random.Range(0, EnglishSecond.Length)];
		}
		#region Stupid Name Arrays
		private static readonly string[] StupidFirst =
		{
			"Big", "Lil", "Fat", "Wet", "Dry", "Hot", "Raw", "Pro", "Noob",
			"Red", "Blue", "Odd", "Wild", "Loud", "Lewd", "Nude", "Sexy", "Kinky",
			"Dirty", "High", "Thick", "Deep", "Tight", "Loose", "Sticky", "Creamy",
			"Juicy", "Salty", "Spicy", "Naked", "Naughty", "Chubby", "Hairy", "Bald",
			"Lazy", "Crazy", "Easy", "Hard", "Soft", "Pink", "Dark", "Holy", "Mega"
		};
		private static readonly string[] StupidSecond =
		{
			"Boi", "Girl", "Man", "Dude", "Dad", "Mom", "Bro", "Sis", "Cat", "Dog",
			"Rat", "Bat", "Pig", "Cow", "Ass", "Butt", "Toe", "Lip", "Egg", "Pie",
			"Bun", "Gun", "God", "Bot", "Simp", "Chad", "Wife", "Bear", "Duck", "Fox",
			"Wolf", "Lion", "Fish", "Crab", "Worm", "Fly", "Bee", "King", "Lord",
			"Boss", "Chief", "Thug", "Goon", "Cop", "Doc", "Hole", "Pole", "Bone", "Ball"
		};
		#endregion

		private static string GenerateStupidName()
		{
			if (Random.value < 0.2f)
				return $"Player{Random.Range(1000, 100000)}";
			
			string first = StupidFirst[Random.Range(0, StupidFirst.Length)];
			string second = StupidSecond[Random.Range(0, StupidSecond.Length)];
			string name = $"{first}{second}";

			float decorationRoll = Random.value;
			if (decorationRoll < 0.1f)
				name = $"xX_{name}_Xx";
			else if (decorationRoll < 0.2f)
				name = $"_{name}_";
			else if (decorationRoll < 0.3f)
				name = name.ToLower();
			else if (decorationRoll < 0.4f)
				name = name.ToUpper();
			else if (decorationRoll < 0.5f)
				name = $"Itz{name}";
			else if (decorationRoll < 0.6f)
				name = $"iAm{name}";

			if (Random.value < 0.3f)
				name += Random.Range(0, 1000).ToString();
				
			return name;
		}
	}
}