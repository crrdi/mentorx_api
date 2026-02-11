-- Row Level Security (RLS) Policies
-- Bu script'i Supabase SQL Editor'de çalıştırın

-- Users Tablosu RLS
ALTER TABLE public.users ENABLE ROW LEVEL SECURITY;

-- Policy: Kullanıcılar sadece kendi profilini görebilir
CREATE POLICY "Users can read own profile"
ON public.users
FOR SELECT
USING (auth.uid() = id AND "deletedAt" IS NULL);

-- Policy: Kullanıcılar sadece kendi profilini güncelleyebilir
CREATE POLICY "Users can update own profile"
ON public.users
FOR UPDATE
USING (auth.uid() = id)
WITH CHECK (auth.uid() = id);

-- Policy: Kullanıcılar kendi profilini oluşturabilir
CREATE POLICY "Users can create own profile"
ON public.users
FOR INSERT
WITH CHECK (auth.uid() = id);

-- Mentors Tablosu RLS
ALTER TABLE public.mentors ENABLE ROW LEVEL SECURITY;

-- Policy: Herkes mentor'ları görebilir (deletedAt IS NULL)
CREATE POLICY "Anyone can read mentors"
ON public.mentors
FOR SELECT
USING ("deletedAt" IS NULL);

-- Policy: Sadece mentor sahibi güncelleyebilir
CREATE POLICY "Mentor owners can update"
ON public.mentors
FOR UPDATE
USING (auth.uid() = "createdBy")
WITH CHECK (auth.uid() = "createdBy");

-- Policy: Authenticated kullanıcılar mentor oluşturabilir
CREATE POLICY "Authenticated users can create mentors"
ON public.mentors
FOR INSERT
WITH CHECK (auth.uid() = "createdBy");

-- Insights Tablosu RLS
ALTER TABLE public.insights ENABLE ROW LEVEL SECURITY;

-- Policy: Herkes insight'ları görebilir
CREATE POLICY "Anyone can read insights"
ON public.insights
FOR SELECT
USING ("deletedAt" IS NULL);

-- Policy: Sadece mentor sahibi post oluşturabilir
CREATE POLICY "Mentor owners can create insights"
ON public.insights
FOR INSERT
WITH CHECK (
  EXISTS (
    SELECT 1 FROM public.mentors m
    WHERE m.id = "mentorId" 
    AND m."createdBy" = auth.uid()
    AND m."deletedAt" IS NULL
  )
);

-- Policy: Sadece mentor sahibi insight güncelleyebilir
CREATE POLICY "Mentor owners can update insights"
ON public.insights
FOR UPDATE
USING (
  EXISTS (
    SELECT 1 FROM public.mentors m
    WHERE m.id = "mentorId" 
    AND m."createdBy" = auth.uid()
    AND m."deletedAt" IS NULL
  )
)
WITH CHECK (
  EXISTS (
    SELECT 1 FROM public.mentors m
    WHERE m.id = "mentorId" 
    AND m."createdBy" = auth.uid()
    AND m."deletedAt" IS NULL
  )
);

-- UserFollowsMentor Tablosu RLS
ALTER TABLE public."UserFollowsMentor" ENABLE ROW LEVEL SECURITY;

-- Policy: Kullanıcılar kendi takip kayıtlarını görebilir
CREATE POLICY "Users can read own follows"
ON public."UserFollowsMentor"
FOR SELECT
USING (auth.uid() = "userId");

-- Policy: Kullanıcılar takip ekleyebilir
CREATE POLICY "Users can follow mentors"
ON public."UserFollowsMentor"
FOR INSERT
WITH CHECK (auth.uid() = "userId");

-- Policy: Kullanıcılar takipten çıkabilir
CREATE POLICY "Users can unfollow mentors"
ON public."UserFollowsMentor"
FOR DELETE
USING (auth.uid() = "userId");

-- Comments Tablosu RLS
ALTER TABLE public.comments ENABLE ROW LEVEL SECURITY;

-- Policy: Herkes yorumları görebilir
CREATE POLICY "Anyone can read comments"
ON public.comments
FOR SELECT
USING ("deletedAt" IS NULL);

-- Policy: Authenticated kullanıcılar yorum ekleyebilir
CREATE POLICY "Authenticated users can create comments"
ON public.comments
FOR INSERT
WITH CHECK (
  auth.uid() IS NOT NULL AND
  EXISTS (
    SELECT 1 FROM public.actors a
    WHERE a.id = "authorActorId"
    AND (
      (a.type = 'user' AND a."userId" = auth.uid()) OR
      (a.type = 'mentor' AND EXISTS (
        SELECT 1 FROM public.mentors m
        WHERE m.id = a."mentorId" AND m."createdBy" = auth.uid()
      ))
    )
  )
);

-- Conversations Tablosu RLS
ALTER TABLE public.conversations ENABLE ROW LEVEL SECURITY;

-- Policy: Kullanıcılar sadece kendi konuşmalarını görebilir
CREATE POLICY "Users can read own conversations"
ON public.conversations
FOR SELECT
USING (auth.uid() = "userId");

-- Policy: Kullanıcılar konuşma oluşturabilir
CREATE POLICY "Users can create conversations"
ON public.conversations
FOR INSERT
WITH CHECK (auth.uid() = "userId");

-- Messages Tablosu RLS
ALTER TABLE public.messages ENABLE ROW LEVEL SECURITY;

-- Policy: Kullanıcılar sadece kendi konuşmalarındaki mesajları görebilir
CREATE POLICY "Users can read own conversation messages"
ON public.messages
FOR SELECT
USING (
  EXISTS (
    SELECT 1 FROM public.conversations c
    WHERE c.id = "conversationId" AND c."userId" = auth.uid()
  )
);

-- Policy: Authenticated kullanıcılar mesaj gönderebilir
CREATE POLICY "Authenticated users can create messages"
ON public.messages
FOR INSERT
WITH CHECK (
  auth.uid() IS NOT NULL AND
  EXISTS (
    SELECT 1 FROM public.conversations c
    WHERE c.id = "conversationId" AND c."userId" = auth.uid()
  ) AND
  EXISTS (
    SELECT 1 FROM public.actors a
    WHERE a.id = "senderActorId"
    AND (
      (a.type = 'user' AND a."userId" = auth.uid()) OR
      (a.type = 'mentor' AND EXISTS (
        SELECT 1 FROM public.mentors m
        WHERE m.id = a."mentorId" AND m."createdBy" = auth.uid()
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
USING (auth.uid() = "userId");

-- Policy: Kullanıcılar beğeni ekleyebilir
CREATE POLICY "Users can like insights"
ON public."UserLikes"
FOR INSERT
WITH CHECK (auth.uid() = "userId");

-- Policy: Kullanıcılar beğeniyi kaldırabilir
CREATE POLICY "Users can unlike insights"
ON public."UserLikes"
FOR DELETE
USING (auth.uid() = "userId");

-- CreditPackages Tablosu RLS
ALTER TABLE public."CreditPackages" ENABLE ROW LEVEL SECURITY;

-- Policy: Herkes kredi paketlerini görebilir
CREATE POLICY "Anyone can read credit packages"
ON public."CreditPackages"
FOR SELECT
USING ("deletedAt" IS NULL);

-- CreditTransactions Tablosu RLS
ALTER TABLE public."CreditTransactions" ENABLE ROW LEVEL SECURITY;

-- Policy: Kullanıcılar sadece kendi transaction'larını görebilir
CREATE POLICY "Users can read own transactions"
ON public."CreditTransactions"
FOR SELECT
USING (auth.uid() = "userId");
