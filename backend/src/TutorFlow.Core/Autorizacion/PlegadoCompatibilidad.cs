using System.Text;

namespace TutorFlow.Core.Autorizacion;

internal static class PlegadoCompatibilidad
{
    private const char SeparadorDeEquivalencias = '¦';
    private const char SeparadorDeInicio = '=';
    private const int BaseHexadecimal = 16;

    private static readonly char[] SaltosDeLinea = ['\r', '\n'];

    private static readonly Dictionary<int, string> Equivalencias = Cargar(TablaNfkc);

    public static string Aplicar(string texto) => string.Concat(texto.EnumerateRunes().Select(De));

    public static string De(Rune rune) =>
        Equivalencias.GetValueOrDefault(rune.Value) ?? rune.ToString();

    private static Dictionary<int, string> Cargar(string tabla) =>
        tabla.Split(SaltosDeLinea, StringSplitOptions.RemoveEmptyEntries)
            .Select(linea => linea.Split(SeparadorDeInicio, 2))
            .SelectMany(partes => partes[1].Split(SeparadorDeEquivalencias)
                .Select((equivalencia, posicion) => (Punto: Convert.ToInt32(partes[0], BaseHexadecimal) + posicion, Equivalencia: equivalencia)))
            .ToDictionary(entrada => entrada.Punto, entrada => entrada.Equivalencia);

    private const string TablaNfkc = """
AA=a
B2=2¦3
B9=1¦o
BC=1⁄4¦1⁄2¦3⁄4
132=IJ¦ij
13F=L·¦l·
17F=s
1C4=DŽ¦Dž¦dž¦LJ¦Lj¦lj¦NJ¦Nj¦nj
1F1=DZ¦Dz¦dz
2B0=h¦ɦ¦j¦r¦ɹ¦ɻ¦ʁ¦w¦y
2E0=ɣ¦l¦s¦x¦ʕ
1D2C=A¦Æ¦B
1D30=D¦E¦Ǝ¦G¦H¦I¦J¦K¦L¦M¦N
1D3C=O¦Ȣ¦P¦R¦T¦U¦W¦a¦ɐ¦ɑ¦ᴂ¦b¦d¦e¦ə¦ɛ¦ɜ¦g
1D4F=k¦m¦ŋ¦o¦ɔ¦ᴖ¦ᴗ¦p¦t¦u¦ᴝ¦ɯ¦v¦ᴥ
1D62=i¦r¦u¦v
1D9B=ɒ¦c¦ɕ¦ð¦ɜ¦f¦ɟ¦ɡ¦ɥ¦ɨ¦ɩ¦ɪ¦ᵻ¦ʝ¦ɭ¦ᶅ¦ʟ¦ɱ¦ɰ¦ɲ¦ɳ¦ɴ¦ɵ¦ɸ¦ʂ¦ʃ¦ƫ¦ʉ¦ʊ¦ᴜ¦ʋ¦ʌ¦z¦ʐ¦ʑ¦ʒ
1E9B=ṡ
2024=.¦..¦...
203C=!!
2047=??¦?!¦!?
2070=0¦i
2074=4¦5¦6¦7¦8¦9¦+
207C==¦(¦)¦n¦0¦1¦2¦3¦4¦5¦6¦7¦8¦9¦+
208C==¦(¦)
2090=a¦e¦o¦x¦ə¦h¦k¦l¦m¦n¦p¦s¦t
20A8=Rs
2100=a/c¦a/s¦C¦°C
2105=c/o¦c/u¦Ɛ
2109=°F¦g¦H¦H¦H¦h¦ħ¦I¦I¦L¦l
2115=N¦No
2119=P¦Q¦R¦R¦R
2120=SM¦TEL¦TM
2124=Z
2128=Z
212C=B¦C
212F=e¦E¦F
2133=M¦o
2139=i
213B=FAX
2145=D¦d¦e¦i¦j
2150=1⁄7¦1⁄9¦1⁄10¦1⁄3¦2⁄3¦1⁄5¦2⁄5¦3⁄5¦4⁄5¦1⁄6¦5⁄6¦1⁄8¦3⁄8¦5⁄8¦7⁄8¦1⁄¦I¦II¦III¦IV¦V¦VI¦VII¦VIII¦IX¦X¦XI¦XII¦L¦C¦D¦M¦i¦ii¦iii¦iv¦v¦vi¦vii¦viii¦ix¦x¦xi¦xii¦l¦c¦d¦m
2189=0⁄3
2460=1¦2¦3¦4¦5¦6¦7¦8¦9¦10¦11¦12¦13¦14¦15¦16¦17¦18¦19¦20¦(1)¦(2)¦(3)¦(4)¦(5)¦(6)¦(7)¦(8)¦(9)¦(10)¦(11)¦(12)¦(13)¦(14)¦(15)¦(16)¦(17)¦(18)¦(19)¦(20)¦1.¦2.¦3.¦4.¦5.¦6.¦7.¦8.¦9.¦10.¦11.¦12.¦13.¦14.¦15.¦16.¦17.¦18.¦19.¦20.¦(a)¦(b)¦(c)¦(d)¦(e)¦(f)¦(g)¦(h)¦(i)¦(j)¦(k)¦(l)¦(m)¦(n)¦(o)¦(p)¦(q)¦(r)¦(s)¦(t)¦(u)¦(v)¦(w)¦(x)¦(y)¦(z)¦A¦B¦C¦D¦E¦F¦G¦H¦I¦J¦K¦L¦M¦N¦O¦P¦Q¦R¦S¦T¦U¦V¦W¦X¦Y¦Z¦a¦b¦c¦d¦e¦f¦g¦h¦i¦j¦k¦l¦m¦n¦o¦p¦q¦r¦s¦t¦u¦v¦w¦x¦y¦z¦0
2A74=::=¦==¦===
2C7C=j¦V
3250=PTE¦21¦22¦23¦24¦25¦26¦27¦28¦29¦30¦31¦32¦33¦34¦35
32B1=36¦37¦38¦39¦40¦41¦42¦43¦44¦45¦46¦47¦48¦49¦50
32CC=Hg¦erg¦eV¦LTD
3371=hPa¦da¦AU¦bar¦oV¦pc¦dm¦dm2¦dm3¦IU
3380=pA¦nA
3383=mA¦kA¦KB¦MB¦GB¦cal¦kcal¦pF¦nF
338E=mg¦kg¦Hz¦kHz¦MHz¦GHz¦THz
3396=ml¦dl¦kl¦fm¦nm
339C=mm¦cm¦km¦mm2¦cm2¦m2¦km2¦mm3¦cm3¦m3¦km3¦m∕s¦m∕s2¦Pa¦kPa¦MPa¦GPa¦rad¦rad∕s¦rad∕s2¦ps¦ns
33B3=ms¦pV¦nV
33B7=mV¦kV¦MV¦pW¦nW
33BD=mW¦kW¦MW
33C2=a.m.¦Bq¦cc¦cd¦C∕kg¦Co.¦dB¦Gy¦ha¦HP¦in¦KK¦KM¦kt¦lm¦ln¦log¦lx¦mb¦mil¦mol¦PH¦p.m.¦PPM¦PR¦sr¦Sv¦Wb¦V∕m¦A∕m
33FF=gal
A770=ꝯ
A7F1=S¦C¦F¦Q
A7F8=Ħ¦œ
AB5C=ꜧ¦ꬷ¦ɫ¦ꭒ
AB69=ʍ
FB00=ff¦fi¦fl¦ffi¦ffl¦st¦st
FB29=+
FE10=,
FE13=:¦;¦!¦?
FE19=...
FE30=..
FE33=_¦_¦(¦)¦{¦}
FE47=[¦]
FE4D=_¦_¦_¦,
FE52=.
FE54=;¦:¦?¦!
FE59=(¦)¦{¦}
FE5F=#¦&¦*¦+¦-¦<¦>¦=
FE68=\¦$¦%¦@
FF01=!¦"¦#¦$¦%¦&¦'¦(¦)¦*¦+¦,¦-¦.¦/¦0¦1¦2¦3¦4¦5¦6¦7¦8¦9¦:¦;¦<¦=¦>¦?¦@¦A¦B¦C¦D¦E¦F¦G¦H¦I¦J¦K¦L¦M¦N¦O¦P¦Q¦R¦S¦T¦U¦V¦W¦X¦Y¦Z¦[¦\¦]¦^¦_¦`¦a¦b¦c¦d¦e¦f¦g¦h¦i¦j¦k¦l¦m¦n¦o¦p¦q¦r¦s¦t¦u¦v¦w¦x¦y¦z¦{¦|¦}¦~
10783=æ¦ʙ¦ɓ
10787=ʣ¦ꭦ¦ʥ¦ʤ¦ɖ¦ɗ¦ᶑ¦ɘ¦ɞ¦ʩ¦ɤ¦ɢ¦ɠ¦ʛ¦ħ¦ʜ¦ɧ¦ʄ¦ʪ¦ʫ¦ɬ¦𝼄¦ꞎ¦ɮ¦𝼅¦ʎ¦𝼆¦ø¦ɶ¦ɷ¦q¦ɺ¦𝼈¦ɽ¦ɾ¦ʀ¦ʨ¦ʦ¦ꭧ¦ʧ¦ʈ¦ⱱ
107B2=ʏ¦ʡ¦ʢ¦ʘ¦ǀ¦ǁ¦ǂ¦𝼊¦𝼞
1CCD6=A¦B¦C¦D¦E¦F¦G¦H¦I¦J¦K¦L¦M¦N¦O¦P¦Q¦R¦S¦T¦U¦V¦W¦X¦Y¦Z¦0¦1¦2¦3¦4¦5¦6¦7¦8¦9
1D400=A¦B¦C¦D¦E¦F¦G¦H¦I¦J¦K¦L¦M¦N¦O¦P¦Q¦R¦S¦T¦U¦V¦W¦X¦Y¦Z¦a¦b¦c¦d¦e¦f¦g¦h¦i¦j¦k¦l¦m¦n¦o¦p¦q¦r¦s¦t¦u¦v¦w¦x¦y¦z¦A¦B¦C¦D¦E¦F¦G¦H¦I¦J¦K¦L¦M¦N¦O¦P¦Q¦R¦S¦T¦U¦V¦W¦X¦Y¦Z¦a¦b¦c¦d¦e¦f¦g
1D456=i¦j¦k¦l¦m¦n¦o¦p¦q¦r¦s¦t¦u¦v¦w¦x¦y¦z¦A¦B¦C¦D¦E¦F¦G¦H¦I¦J¦K¦L¦M¦N¦O¦P¦Q¦R¦S¦T¦U¦V¦W¦X¦Y¦Z¦a¦b¦c¦d¦e¦f¦g¦h¦i¦j¦k¦l¦m¦n¦o¦p¦q¦r¦s¦t¦u¦v¦w¦x¦y¦z¦A
1D49E=C¦D
1D4A2=G
1D4A5=J¦K
1D4A9=N¦O¦P¦Q
1D4AE=S¦T¦U¦V¦W¦X¦Y¦Z¦a¦b¦c¦d
1D4BB=f
1D4BD=h¦i¦j¦k¦l¦m¦n
1D4C5=p¦q¦r¦s¦t¦u¦v¦w¦x¦y¦z¦A¦B¦C¦D¦E¦F¦G¦H¦I¦J¦K¦L¦M¦N¦O¦P¦Q¦R¦S¦T¦U¦V¦W¦X¦Y¦Z¦a¦b¦c¦d¦e¦f¦g¦h¦i¦j¦k¦l¦m¦n¦o¦p¦q¦r¦s¦t¦u¦v¦w¦x¦y¦z¦A¦B
1D507=D¦E¦F¦G
1D50D=J¦K¦L¦M¦N¦O¦P¦Q
1D516=S¦T¦U¦V¦W¦X¦Y
1D51E=a¦b¦c¦d¦e¦f¦g¦h¦i¦j¦k¦l¦m¦n¦o¦p¦q¦r¦s¦t¦u¦v¦w¦x¦y¦z¦A¦B
1D53B=D¦E¦F¦G
1D540=I¦J¦K¦L¦M
1D546=O
1D54A=S¦T¦U¦V¦W¦X¦Y
1D552=a¦b¦c¦d¦e¦f¦g¦h¦i¦j¦k¦l¦m¦n¦o¦p¦q¦r¦s¦t¦u¦v¦w¦x¦y¦z¦A¦B¦C¦D¦E¦F¦G¦H¦I¦J¦K¦L¦M¦N¦O¦P¦Q¦R¦S¦T¦U¦V¦W¦X¦Y¦Z¦a¦b¦c¦d¦e¦f¦g¦h¦i¦j¦k¦l¦m¦n¦o¦p¦q¦r¦s¦t¦u¦v¦w¦x¦y¦z¦A¦B¦C¦D¦E¦F¦G¦H¦I¦J¦K¦L¦M¦N¦O¦P¦Q¦R¦S¦T¦U¦V¦W¦X¦Y¦Z¦a¦b¦c¦d¦e¦f¦g¦h¦i¦j¦k¦l¦m¦n¦o¦p¦q¦r¦s¦t¦u¦v¦w¦x¦y¦z¦A¦B¦C¦D¦E¦F¦G¦H¦I¦J¦K¦L¦M¦N¦O¦P¦Q¦R¦S¦T¦U¦V¦W¦X¦Y¦Z¦a¦b¦c¦d¦e¦f¦g¦h¦i¦j¦k¦l¦m¦n¦o¦p¦q¦r¦s¦t¦u¦v¦w¦x¦y¦z¦A¦B¦C¦D¦E¦F¦G¦H¦I¦J¦K¦L¦M¦N¦O¦P¦Q¦R¦S¦T¦U¦V¦W¦X¦Y¦Z¦a¦b¦c¦d¦e¦f¦g¦h¦i¦j¦k¦l¦m¦n¦o¦p¦q¦r¦s¦t¦u¦v¦w¦x¦y¦z¦A¦B¦C¦D¦E¦F¦G¦H¦I¦J¦K¦L¦M¦N¦O¦P¦Q¦R¦S¦T¦U¦V¦W¦X¦Y¦Z¦a¦b¦c¦d¦e¦f¦g¦h¦i¦j¦k¦l¦m¦n¦o¦p¦q¦r¦s¦t¦u¦v¦w¦x¦y¦z¦A¦B¦C¦D¦E¦F¦G¦H¦I¦J¦K¦L¦M¦N¦O¦P¦Q¦R¦S¦T¦U¦V¦W¦X¦Y¦Z¦a¦b¦c¦d¦e¦f¦g¦h¦i¦j¦k¦l¦m¦n¦o¦p¦q¦r¦s¦t¦u¦v¦w¦x¦y¦z¦ı¦ȷ
1D7CE=0¦1¦2¦3¦4¦5¦6¦7¦8¦9¦0¦1¦2¦3¦4¦5¦6¦7¦8¦9¦0¦1¦2¦3¦4¦5¦6¦7¦8¦9¦0¦1¦2¦3¦4¦5¦6¦7¦8¦9¦0¦1¦2¦3¦4¦5¦6¦7¦8¦9
1F100=0.¦0,¦1,¦2,¦3,¦4,¦5,¦6,¦7,¦8,¦9,
1F110=(A)¦(B)¦(C)¦(D)¦(E)¦(F)¦(G)¦(H)¦(I)¦(J)¦(K)¦(L)¦(M)¦(N)¦(O)¦(P)¦(Q)¦(R)¦(S)¦(T)¦(U)¦(V)¦(W)¦(X)¦(Y)¦(Z)¦〔S〕¦C¦R¦CD¦WZ
1F130=A¦B¦C¦D¦E¦F¦G¦H¦I¦J¦K¦L¦M¦N¦O¦P¦Q¦R¦S¦T¦U¦V¦W¦X¦Y¦Z¦HV¦MV¦SD¦SS¦PPV¦WC
1F16A=MC¦MD¦MR
1F190=DJ
1FBF0=0¦1¦2¦3¦4¦5¦6¦7¦8¦9
""";
}
