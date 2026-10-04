using Monopoly.Server.Domain;

namespace Monopoly.Server.Data;

public static class Board
{
    public static readonly string[] Tokens = ["car", "hat", "dog", "ship", "thimble", "boot", "cat", "moneybag", "duck", "penguin", "wood-orange", "wood-mushroom", "wood-pear", "wood-pawn", "wood-bottle", "wood-candle"];
    public static readonly Square[] Squares =
    [
        new(0,"GO","VIA","go"),
        new(1,"Mediterranean Avenue","Vicolo Corto","street","brown","#955436",60,[2,10,30,90,160,250],50),
        new(2,"Community Chest","Probabilità","chest"),
        new(3,"Baltic Avenue","Vicolo Stretto","street","brown","#955436",60,[4,20,60,180,320,450],50),
        new(4,"Income Tax","Tassa patrimoniale","tax",Price:200),
        new(5,"Reading Railroad","Stazione Sud","rail",Price:200,Rents:[25,50,100,200]),
        new(6,"Oriental Avenue","Viale Monterosa","street","sky","#9ed8e9",100,[6,30,90,270,400,550],50),
        new(7,"Chance","Imprevisti","chance"),
        new(8,"Vermont Avenue","Viale Vesuvio","street","sky","#9ed8e9",100,[6,30,90,270,400,550],50),
        new(9,"Connecticut Avenue","Viale Gran Sasso","street","sky","#9ed8e9",120,[8,40,100,300,450,600],50),
        new(10,"Jail / Just Visiting","Prigione / Transito","jail"),
        new(11,"St. Charles Place","Via Accademia","street","pink","#d94791",140,[10,50,150,450,625,750],100),
        new(12,"Electric Company","Società elettrica","utility",Price:150),
        new(13,"States Avenue","Corso Ateneo","street","pink","#d94791",140,[10,50,150,450,625,750],100),
        new(14,"Virginia Avenue","Piazza Università","street","pink","#d94791",160,[12,60,180,500,700,900],100),
        new(15,"Pennsylvania Railroad","Stazione Ovest","rail",Price:200,Rents:[25,50,100,200]),
        new(16,"St. James Place","Via Verdi","street","orange","#ef9035",180,[14,70,200,550,750,950],100),
        new(17,"Community Chest","Probabilità","chest"),
        new(18,"Tennessee Avenue","Corso Raffaello","street","orange","#ef9035",180,[14,70,200,550,750,950],100),
        new(19,"New York Avenue","Piazza Dante","street","orange","#ef9035",200,[16,80,220,600,800,1000],100),
        new(20,"Free Parking","Posteggio gratuito","parking"),
        new(21,"Kentucky Avenue","Via Marco Polo","street","red","#d73942",220,[18,90,250,700,875,1050],150),
        new(22,"Chance","Imprevisti","chance"),
        new(23,"Indiana Avenue","Corso Magellano","street","red","#d73942",220,[18,90,250,700,875,1050],150),
        new(24,"Illinois Avenue","Largo Colombo","street","red","#d73942",240,[20,100,300,750,925,1100],150),
        new(25,"B. & O. Railroad","Stazione Nord","rail",Price:200,Rents:[25,50,100,200]),
        new(26,"Atlantic Avenue","Viale Costantino","street","yellow","#f1d84d",260,[22,110,330,800,975,1150],150),
        new(27,"Ventnor Avenue","Viale Traiano","street","yellow","#f1d84d",260,[22,110,330,800,975,1150],150),
        new(28,"Water Works","Società acqua potabile","utility",Price:150),
        new(29,"Marvin Gardens","Piazza Giulio Cesare","street","yellow","#f1d84d",280,[24,120,360,850,1025,1200],150),
        new(30,"Go To Jail","In prigione!","goToJail"),
        new(31,"Pacific Avenue","Via Roma","street","green","#349267",300,[26,130,390,900,1100,1275],200),
        new(32,"North Carolina Avenue","Corso Impero","street","green","#349267",300,[26,130,390,900,1100,1275],200),
        new(33,"Community Chest","Probabilità","chest"),
        new(34,"Pennsylvania Avenue","Largo Augusto","street","green","#349267",320,[28,150,450,1000,1200,1400],200),
        new(35,"Short Line","Stazione Est","rail",Price:200,Rents:[25,50,100,200]),
        new(36,"Chance","Imprevisti","chance"),
        new(37,"Park Place","Viale dei Giardini","street","blue","#28559d",350,[35,175,500,1100,1300,1500],200),
        new(38,"Luxury Tax","Tassa di lusso","tax",Price:100),
        new(39,"Boardwalk","Parco della Vittoria","street","blue","#28559d",400,[50,200,600,1400,1700,2000],200)
    ];
}
