using QuizArena.Entities.Enums;

namespace QuizArena.DAL.Seed;

/// <summary>
/// İngilizce başlangıç içeriği.
/// </summary>
/// <remarks>
/// <para>
/// Sorular Türkçe setin birebir çevirisi değil. Türkçe havuzda Türkiye'ye
/// özgü sorular var ("Türk alfabesinde kaç harf var?"); bunlar İngilizce
/// oynayan birine anlamlı gelmez. Zorluk dağılımı ve kategori yapısı aynı
/// tutulup içerik hedef kitleye uyarlandı.
/// </para>
/// <para>
/// Hangi setin yükleneceğini <c>Seed:Language</c> ayarı belirler; ikisi bir
/// arada yüklenmez, çünkü karışık dilli bir soru havuzunda oyun oynanamaz.
/// </para>
/// </remarks>
internal static class SeedContentEnglish
{
    internal static SeedCategory[] Categories =>
    [
        new("General Knowledge", "general-knowledge",
            "A bit of everything: geography, science, everyday facts and history.",
            "🧠", "#6366F1", 1,
            [
                new("Which planet is closest to the Sun?",
                    QuestionDifficulty.Easy, null,
                    ["Venus", "Mercury", "Mars", "Earth"], 1),

                new("How many minutes are there in a full football match?",
                    QuestionDifficulty.Easy, "Two halves of 45 minutes each.",
                    ["60", "80", "90", "120"], 2),

                new("How many letters are there in the English alphabet?",
                    QuestionDifficulty.Easy, null,
                    ["24", "25", "26", "28"], 2),

                new("Which is the largest planet in the solar system?",
                    QuestionDifficulty.Easy, null,
                    ["Saturn", "Jupiter", "Neptune", "Earth"], 1),

                new("What is the chemical formula for water?",
                    QuestionDifficulty.Easy, "Two hydrogen atoms and one oxygen atom.",
                    ["CO₂", "H₂O", "O₂", "NaCl"], 1),

                new("In which year did the Second World War end?",
                    QuestionDifficulty.Easy, "Germany surrendered in May and Japan in September 1945.",
                    ["1943", "1944", "1945", "1946"], 2),

                new("How many months of the year have 31 days?",
                    QuestionDifficulty.Medium,
                    "January, March, May, July, August, October and December.",
                    ["5", "6", "7", "8"], 2),

                new("Which is the largest ocean on Earth?",
                    QuestionDifficulty.Medium, null,
                    ["Atlantic Ocean", "Indian Ocean", "Pacific Ocean", "Arctic Ocean"], 2)
            ]),

        new("History", "history",
            "Turning points from the ancient world to the twentieth century.",
            "🏛️", "#B45309", 2,
            [
                new("In which year did Constantinople fall to the Ottomans?",
                    QuestionDifficulty.Easy, "29 May 1453, under Mehmed II.",
                    ["1071", "1299", "1453", "1517"], 2),

                new("Who was the first President of the United States?",
                    QuestionDifficulty.Easy, null,
                    ["Thomas Jefferson", "George Washington", "John Adams", "Benjamin Franklin"], 1),

                new("In which year did the Berlin Wall fall?",
                    QuestionDifficulty.Medium, "The border was opened on 9 November 1989.",
                    ["1961", "1985", "1989", "1991"], 2),

                new("Which empire built the road network centred on Rome?",
                    QuestionDifficulty.Medium, null,
                    ["Greek", "Roman", "Persian", "Byzantine"], 1),

                new("In which year did the First World War begin?",
                    QuestionDifficulty.Easy, null,
                    ["1912", "1914", "1918", "1939"], 1),

                new("Which document did King John of England seal in 1215?",
                    QuestionDifficulty.Medium, "Magna Carta limited the powers of the crown.",
                    ["Bill of Rights", "Magna Carta", "Domesday Book", "Act of Union"], 1),

                new("In which year did the French Revolution begin?",
                    QuestionDifficulty.Medium, null,
                    ["1776", "1789", "1804", "1848"], 1),

                new("Who was the first woman to win a Nobel Prize?",
                    QuestionDifficulty.Hard,
                    "Marie Curie won the Nobel Prize in Physics in 1903.",
                    ["Marie Curie", "Rosalind Franklin", "Ada Lovelace", "Dorothy Hodgkin"], 0)
            ]),

        new("Geography", "geography",
            "Mountains, rivers, capitals and the shape of the world.",
            "🌍", "#059669", 3,
            [
                new("Which is the longest river in the world?",
                    QuestionDifficulty.Medium,
                    "The Nile is usually given as the longest, though the Amazon is close.",
                    ["Amazon", "Yangtze", "Nile", "Mississippi"], 2),

                new("Which is the highest mountain above sea level?",
                    QuestionDifficulty.Easy, "Everest, 8,849 metres.",
                    ["K2", "Everest", "Kangchenjunga", "Denali"], 1),

                new("Which is the largest lake by surface area?",
                    QuestionDifficulty.Medium,
                    "The Caspian Sea is classified as a lake despite its name.",
                    ["Lake Superior", "Lake Victoria", "Caspian Sea", "Lake Baikal"], 2),

                new("What is the capital of Japan?",
                    QuestionDifficulty.Easy, null,
                    ["Osaka", "Kyoto", "Tokyo", "Nagoya"], 2),

                new("Which is the longest river in Africa?",
                    QuestionDifficulty.Medium, null,
                    ["Congo", "Nile", "Niger", "Zambezi"], 1),

                new("How many continents are there?",
                    QuestionDifficulty.Easy, null,
                    ["5", "6", "7", "8"], 2),

                new("Which of these countries does NOT have a coastline on the Black Sea?",
                    QuestionDifficulty.Hard,
                    "Its coast is shared by Türkiye, Bulgaria, Romania, Ukraine, Russia and Georgia.",
                    ["Georgia", "Bulgaria", "Romania", "Serbia"], 3),

                new("Which continent does the Equator NOT cross?",
                    QuestionDifficulty.Medium, null,
                    ["Africa", "South America", "Asia", "Europe"], 3)
            ]),

        new("Science and Technology", "science-technology",
            "Physics, chemistry, biology and the world of computing.",
            "🔬", "#0891B2", 4,
            [
                new("At what temperature does water boil at sea level?",
                    QuestionDifficulty.Easy, "100 °C, or 212 °F.",
                    ["90 °C", "95 °C", "100 °C", "110 °C"], 2),

                new("Which is the largest organ in the human body?",
                    QuestionDifficulty.Medium,
                    "The skin, with a surface area of roughly 2 square metres.",
                    ["Liver", "Skin", "Lung", "Brain"], 1),

                new("Which element does the symbol \"O\" represent?",
                    QuestionDifficulty.Easy, null,
                    ["Gold", "Osmium", "Oxygen", "Nitrogen"], 2),

                new("How many bits are there in one byte?",
                    QuestionDifficulty.Easy, null,
                    ["4", "8", "16", "32"], 1),

                new("Roughly how fast does light travel in a vacuum?",
                    QuestionDifficulty.Medium, "About 299,792 kilometres per second.",
                    ["3,000 km/s", "30,000 km/s", "300,000 km/s", "3,000,000 km/s"], 2),

                new("Which molecule carries genetic information?",
                    QuestionDifficulty.Easy, null,
                    ["ATP", "DNA", "Glucose", "Haemoglobin"], 1),

                new("Who formulated the law of universal gravitation?",
                    QuestionDifficulty.Medium, null,
                    ["Galileo Galilei", "Isaac Newton", "Albert Einstein", "Nikola Tesla"], 1),

                new("Which port does HTTP use by default?",
                    QuestionDifficulty.Hard, "HTTP uses port 80; HTTPS uses 443.",
                    ["21", "25", "80", "443"], 2)
            ]),

        new("Arts and Literature", "arts-literature",
            "Novels, poetry, painting and music.",
            "🎭", "#DB2777", 5,
            [
                new("Who wrote the novel \"Pride and Prejudice\"?",
                    QuestionDifficulty.Medium, null,
                    ["Jane Austen", "Charlotte Brontë", "George Eliot", "Virginia Woolf"], 0),

                new("Who wrote the play \"Hamlet\"?",
                    QuestionDifficulty.Easy, null,
                    ["Christopher Marlowe", "William Shakespeare", "Ben Jonson", "John Webster"], 1),

                new("Who painted the \"Mona Lisa\"?",
                    QuestionDifficulty.Easy, null,
                    ["Michelangelo", "Raphael", "Leonardo da Vinci", "Vincent van Gogh"], 2),

                new("Which novel opens with the line \"Call me Ishmael\"?",
                    QuestionDifficulty.Medium, "The opening of Herman Melville's Moby-Dick.",
                    ["Moby-Dick", "Treasure Island", "The Old Man and the Sea", "Robinson Crusoe"], 0),

                new("Who wrote \"One Hundred Years of Solitude\"?",
                    QuestionDifficulty.Medium, null,
                    ["Gabriel García Márquez", "Jorge Luis Borges", "Isabel Allende", "Mario Vargas Llosa"], 0),

                new("Which artist cut off part of his own ear in 1888?",
                    QuestionDifficulty.Hard, null,
                    ["Vincent van Gogh", "Paul Gauguin", "Edvard Munch", "Henri Matisse"], 0),

                new("Ludwig van Beethoven is famous in which field?",
                    QuestionDifficulty.Easy, null,
                    ["Painting", "Music", "Sculpture", "Architecture"], 1),

                new("Who wrote the dystopian novel \"Nineteen Eighty-Four\"?",
                    QuestionDifficulty.Hard, null,
                    ["Aldous Huxley", "George Orwell", "Ray Bradbury", "H. G. Wells"], 1)
            ]),

        new("Sport", "sport",
            "Football, basketball, the Olympics and more.",
            "⚽", "#EA580C", 6,
            [
                new("How many players from one team are on a basketball court?",
                    QuestionDifficulty.Easy, null,
                    ["4", "5", "6", "7"], 1),

                new("How often are the Summer Olympic Games held?",
                    QuestionDifficulty.Easy, null,
                    ["Every 2 years", "Every 3 years", "Every 4 years", "Every 5 years"], 2),

                new("Which country has won the FIFA World Cup the most times?",
                    QuestionDifficulty.Medium, "Brazil have won it five times.",
                    ["Germany", "Italy", "Argentina", "Brazil"], 3),

                new("How many players from one team are on a volleyball court?",
                    QuestionDifficulty.Easy, null,
                    ["5", "6", "7", "8"], 1),

                new("On what surface is Wimbledon played?",
                    QuestionDifficulty.Medium, null,
                    ["Clay", "Hard court", "Grass", "Carpet"], 2),

                new("Which of these is NOT a tennis Grand Slam tournament?",
                    QuestionDifficulty.Hard,
                    "The four are the Australian Open, Roland Garros, Wimbledon and the US Open.",
                    ["Wimbledon", "Roland Garros", "US Open", "Monte Carlo Masters"], 3),

                new("How many pieces does each player start with in chess?",
                    QuestionDifficulty.Medium, "8 pawns, 2 rooks, 2 knights, 2 bishops, 1 king, 1 queen.",
                    ["12", "14", "16", "18"], 2),

                new("How many points does a Formula 1 race winner score?",
                    QuestionDifficulty.Hard, null,
                    ["10", "18", "25", "30"], 2)
            ])
    ];
}
