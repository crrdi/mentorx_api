-- Row Level Security (RLS) Policies
-- Bu script'i Supabase SQL Editor'de çalıştırın
-- Entity Framework table/column naming convention (PascalCase) kullanılıyor

-- Users Tablosu RLS
ALTER TABLE public."Users" ENABLE ROW LEVEL SECURITY;

-- Policy: Kullanıcılar sadece kendi profilini görebilir
CREATE POLICY "Users can read own profile"
ON public."Users"
FOR SELECT
USING (auth.uid() = "Id" AND "DeletedAt" IS NULL);

-- Policy: Kullanıcılar sadece kendi profilini güncelleyebilir
CREATE POLICY "Users can update own profile"
ON public."Users"
FOR UPDATE
USING (auth.uid() = "Id")
WITH CHECK (auth.uid() = "Id");

-- Policy: Kullanıcılar kendi profilini oluşturabilir
CREATE POLICY "Users can create own profile"
ON public."Users"
FOR INSERT
WITH CHECK (auth.uid() = "Id");

-- Mentors Tablosu RLS
ALTER TABLE public."Mentors" ENABLE ROW LEVEL SECURITY;

-- Policy: Herkes mentor'ları görebilir (DeletedAt IS NULL)
CREATE POLICY "Anyone can read mentors"
ON public."Mentors"
FOR SELECT
USING ("DeletedAt" IS NULL);

-- Policy: Sadece mentor sahibi güncelleyebilir
CREATE POLICY "Mentor owners can update"
ON public."Mentors"
FOR UPDATE
USING (auth.uid() = "CreatedBy")
WITH CHECK (auth.uid() = "CreatedBy");

-- Policy: Authenticated kullanıcılar mentor oluşturabilir
CREATE POLICY "Authenticated users can create mentors"
ON public."Mentors"
FOR INSERT
WITH CHECK (auth.uid() = "CreatedBy");

-- Insights Tablosu RLS
ALTER TABLE public."Insights" ENABLE ROW LEVEL SECURITY;

-- Policy: Herkes insight'ları görebilir
CREATE POLICY "Anyone can read insights"
ON public."Insights"
FOR SELECT
USING ("DeletedAt" IS NULL);

-- Policy: Sadece mentor sahibi post oluşturabilir
CREATE POLICY "Mentor owners can create insights"
ON public."Insights"
FOR INSERT
WITH CHECK (
  EXISTS (
    SELECT 1 FROM public."Mentors" m
    WHERE m."Id" = "MentorId" 
    AND m."CreatedBy" = auth.uid()
    AND m."DeletedAt" IS NULL
  )
);

-- Policy: Sadece mentor sahibi insight güncelleyebilir
CREATE POLICY "Mentor owners can update insights"
ON public."Insights"
FOR UPDATE
USING (
  EXISTS (
    SELECT 1 FROM public."Mentors" m
    WHERE m."Id" = "MentorId" 
    AND m."CreatedBy" = auth.uid()
    AND m."DeletedAt" IS NULL
  )
)
WITH CHECK (
  EXISTS (
    SELECT 1 FROM public."Mentors" m
    WHERE m."Id" = "MentorId" 
    AND m."CreatedBy" = auth.uid()
    AND m."DeletedAt" IS NULL
  )
);

-- UserFollowsMentor Tablosu RLS
ALTER TABLE public."UserFollowsMentor" ENABLE ROW LEVEL SECURITY;

-- Policy: Kullanıcılar kendi takip kayıtlarını görebilir
CREATE POLICY "Users can read own follows"
ON public."UserFollowsMentor"
FOR SELECT
USING (auth.uid() = "UserId");

-- Policy: Kullanıcılar takip ekleyebilir
CREATE POLICY "Users can follow mentors"
ON public."UserFollowsMentor"
FOR INSERT
WITH CHECK (auth.uid() = "UserId");

-- Policy: Kullanıcılar takipten çıkabilir
CREATE POLICY "Users can unfollow mentors"
ON public."UserFollowsMentor"
FOR DELETE
USING (auth.uid() = "UserId");

-- Comments Tablosu RLS
ALTER TABLE public."Comments" ENABLE ROW LEVEL SECURITY;

-- Policy: Herkes yorumları görebilir
CREATE POLICY "Anyone can read comments"
ON public."Comments"
FOR SELECT
USING ("DeletedAt" IS NULL);

-- Policy: Authenticated kullanıcılar yorum ekleyebilir
CREATE POLICY "Authenticated users can create comments"
ON public."Comments"
FOR INSERT
WITH CHECK (
  auth.uid() IS NOT NULL AND
  EXISTS (
    SELECT 1 FROM public."Actors" a
    WHERE a."Id" = "AuthorActorId"
    AND (
      (a."Type" = 1 AND a."UserId" = auth.uid()) OR
      (a."Type" = 2 AND EXISTS (
        SELECT 1 FROM public."Mentors" m
        WHERE m."Id" = a."MentorId" AND m."CreatedBy" = auth.uid()
      ))
    )
  )
);

-- Conversations Tablosu RLS
ALTER TABLE public."Conversations" ENABLE ROW LEVEL SECURITY;

-- Policy: Kullanıcılar sadece kendi konuşmalarını görebilir
CREATE POLICY "Users can read own conversations"
ON public."Conversations"
FOR SELECT
USING (auth.uid() = "UserId");

-- Policy: Kullanıcılar konuşma oluşturabilir
CREATE POLICY "Users can create conversations"
ON public."Conversations"
FOR INSERT
WITH CHECK (auth.uid() = "UserId");

-- Messages Tablosu RLS
ALTER TABLE public."Messages" ENABLE ROW LEVEL SECURITY;

-- Policy: Kullanıcılar sadece kendi konuşmalarındaki mesajları görebilir
CREATE POLICY "Users can read own conversation messages"
ON public."Messages"
FOR SELECT
USING (
  EXISTS (
    SELECT 1 FROM public."Conversations" c
    WHERE c."Id" = "ConversationId" AND c."UserId" = auth.uid()
  )
);

-- Policy: Authenticated kullanıcılar mesaj gönderebilir
CREATE POLICY "Authenticated users can create messages"
ON public."Messages"
FOR INSERT
WITH CHECK (
  auth.uid() IS NOT NULL AND
  EXISTS (
    SELECT 1 FROM public."Conversations" c
    WHERE c."Id" = "ConversationId" AND c."UserId" = auth.uid()
  ) AND
  EXISTS (
    SELECT 1 FROM public."Actors" a
    WHERE a."Id" = "SenderActorId"
    AND (
      (a."Type" = 1 AND a."UserId" = auth.uid()) OR
      (a."Type" = 2 AND EXISTS (
        SELECT 1 FROM public."Mentors" m
        WHERE m."Id" = a."MentorId" AND m."CreatedBy" = auth.uid()
      ))
    )
  )
);

-- UserLikes Tablosu RLS
ALTER TABLE public."UserLikes" ENABLE ROW LEVEL SECURITY;

-- Policy: Kullanıcılar kendi beğenilerini görebilir
CREATE POLICY "Users can read own likes"
ON public."UserLikes"
FOR SELECT
USING (auth.uid() = "UserId");

-- Policy: Kullanıcılar beğeni ekleyebilir
CREATE POLICY "Users can like insights"
ON public."UserLikes"
FOR INSERT
WITH CHECK (auth.uid() = "UserId");

-- Policy: Kullanıcılar beğeniyi kaldırabilir
CREATE POLICY "Users can unlike insights"
ON public."UserLikes"
FOR DELETE
USING (auth.uid() = "UserId");

-- CreditPackages Tablosu RLS
ALTER TABLE public."CreditPackages" ENABLE ROW LEVEL SECURITY;

-- Policy: Herkes kredi paketlerini görebilir
CREATE POLICY "Anyone can read credit packages"
ON public."CreditPackages"
FOR SELECT
USING ("DeletedAt" IS NULL);

-- CreditTransactions Tablosu RLS
ALTER TABLE public."CreditTransactions" ENABLE ROW LEVEL SECURITY;

-- Policy: Kullanıcılar sadece kendi transaction'larını görebilir
CREATE POLICY "Users can read own transactions"
ON public."CreditTransactions"
FOR SELECT
USING (auth.uid() = "UserId");
