FROM node:current-alpine AS builder

WORKDIR /build
COPY . .
RUN yarn
RUN yarn build

FROM node:current-alpine AS app

WORKDIR /app
COPY --from=builder /build/build build/
COPY package.json .
EXPOSE 3000
ENV NODE_ENV=production
ENTRYPOINT ["node", "build", "--input-type=module"]