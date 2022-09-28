FROM node:16.13.1-buster as builder
WORKDIR /usr/src/app
COPY package.json ./
RUN npm install --legacy-peer-deps
COPY . .
RUN npm run build --prod

FROM nginx:1.19.4-alpine
COPY --from=builder /usr/src/app/dist/xServerUI /usr/share/nginx/html
COPY nginx.conf /etc/nginx/nginx.conf
