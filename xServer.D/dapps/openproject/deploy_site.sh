#!/bin/bash

if [ $# -lt 4 ]; then
  echo "Usage: $0 appname domain email my.db.password"
  echo "Usage: $0 openproject openproject.x42.site my@email.com mysecetwppass"
  exit 1
fi

APP_NAME=$1
DOMAIN=$2
DOMAIN_LOWER=$(echo "$DOMAIN" | tr '[:upper:]' '[:lower:]' )
DOMAIN_LOWER_USCORE=$(echo "$DOMAIN" | tr '[:upper:]' '[:lower:]' | sed 's/\./'_'/g')
DOMAIN_STRIPPEDLOWER=$(echo "$DOMAIN_LOWER_USCORE" | sed 's/_//g')
EMAIL=$3
DB_PASSWORD=$4

main(){
	echo "Setting Up ${DOMAIN_LOWER_USCORE}"
	echo Setting up Envrionment
	mkdir -p sites/${DOMAIN}
	sed -e 's/#DOMAIN#/'${DOMAIN}'/g' -e 's/#domain#/'${DOMAIN_LOWER_USCORE}'/g' docker-compose.yml > sites/${DOMAIN}/docker-compose.yml
	cd sites/${DOMAIN}
	mkdir pgdata
	mkdir opdata

cat <<EOF > .env
POSTGRES_PASSWORD=${DB_PASSWORD}
EOF

	docker-compose up -d
		
	echo "Done."
}

main
