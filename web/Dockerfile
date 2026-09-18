# syntax=docker/dockerfile:1
#
# SahaGor web admin paneli production imaji (SG-422). "output: standalone" (next.config.ts)
# sayesinde son imaj, tum node_modules yerine sadece izlenen (traced) dosyalari icerir.

FROM node:20-alpine AS deps
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci

FROM node:20-alpine AS build
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .

# NEXT_PUBLIC_* degiskenleri calisma zamaninda DEGIL, "next build" sirasinda istemci
# paketine gomulur; bu yuzden bir ARG olarak imaj olusturulurken verilmelidir (bkz.
# docker-compose.prod.yml). Degeri degistirmek imajin YENIDEN olusturulmasini gerektirir.
ARG NEXT_PUBLIC_API_BASE_URL
ENV NEXT_PUBLIC_API_BASE_URL=${NEXT_PUBLIC_API_BASE_URL}
RUN npm run build

FROM node:20-alpine AS runtime
WORKDIR /app
ENV NODE_ENV=production

RUN addgroup -S sahagor && adduser -S sahagor -G sahagor

COPY --from=build --chown=sahagor:sahagor /app/public ./public
COPY --from=build --chown=sahagor:sahagor /app/.next/standalone ./
COPY --from=build --chown=sahagor:sahagor /app/.next/static ./.next/static

USER sahagor
ENV PORT=3000
EXPOSE 3000

CMD ["node", "server.js"]
